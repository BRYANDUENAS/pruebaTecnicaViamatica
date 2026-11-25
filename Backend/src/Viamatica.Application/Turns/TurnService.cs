using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Common;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Turns;

public record TurnDto(
    int TurnId,
    string Description,
    DateTime Date,
    int CashId,
    string CashDescription,
    string AttentionTypeId,
    string AttentionTypeDescription,
    int GestorId,
    string GestorUsername,
    int StatusId,
    string StatusDescription,
    int? ClientId,
    string? ClientName,
    string? CashierUsername,
    DateTime? AttendedAt);

public record CreateTurnRequest(int CashId, string AttentionTypeId);

/// <summary>Solo se puede mover un turno pendiente a otra caja.</summary>
public record UpdateTurnRequest(int CashId);

public record FinishTurnRequest(int? ClientId);

public class TurnService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ILogger<TurnService> _logger;

    public TurnService(IAppDbContext db, ICurrentUser current, ILogger<TurnService> logger)
    {
        _db = db;
        _current = current;
        _logger = logger;
    }

    /// <summary>
    /// Cada turno tiene una atención. El alcance depende del rol: el administrador
    /// ve todo, el gestor lo que asignó y el cajero lo de su caja.
    /// </summary>
    private IQueryable<Attention> Scoped()
    {
        var query = _db.Attentions
            .Include(a => a.Turn).ThenInclude(t => t.Cash)
            .Include(a => a.Turn).ThenInclude(t => t.UserGestor)
            .Include(a => a.AttentionType)
            .Include(a => a.Status)
            .Include(a => a.Client)
            .Include(a => a.UserCashier)
            .AsQueryable();

        if (_current.IsGestor) query = query.Where(a => a.Turn.UserGestorId == _current.UserId);
        if (_current.IsCajero) query = query.Where(a => a.Turn.Cash.UserCashes.Any(uc => uc.UserId == _current.UserId));
        return query;
    }

    private static TurnDto Map(Attention a) => new(
        a.TurnId, a.Turn.Description, a.Turn.Date, a.Turn.CashId, a.Turn.Cash.CashDescription,
        a.AttentionTypeId, a.AttentionType.Description, a.Turn.UserGestorId, a.Turn.UserGestor.Username,
        a.StatusId, a.Status.Description, a.ClientId,
        a.Client is null ? null : $"{a.Client.Name} {a.Client.LastName}",
        a.UserCashier?.Username, a.AttendedAt);

    public async Task<List<TurnDto>> ListAsync(DateOnly? date, int? cashId, int? statusId, CancellationToken ct)
    {
        var (from, to) = BusinessDay.RangeUtc(date ?? BusinessDay.Today);
        var query = Scoped().Where(a => a.Turn.Date >= from && a.Turn.Date < to);
        if (cashId is not null) query = query.Where(a => a.Turn.CashId == cashId);
        if (statusId is not null) query = query.Where(a => a.StatusId == statusId);
        var rows = await query.OrderBy(a => a.TurnId).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<TurnDto> GetAsync(int id, CancellationToken ct) => Map(await FindAsync(id, ct));

    // ----- Asignación (gestor) -----

    public async Task<TurnDto> CreateAsync(CreateTurnRequest request, CancellationToken ct)
    {
        if (!await _db.AttentionTypes.AnyAsync(t => t.AttentionTypeId == request.AttentionTypeId, ct))
            throw new DomainException("El tipo de atención no existe.");
        await EnsureCashCanReceiveAsync(request.CashId, ct);

        // La función de base de datos genera la descripción (ej. AC0001) y crea
        // el turno con su atención pendiente en una sola operación.
        var turnId = await _db.CreateTurnAsync(request.AttentionTypeId, request.CashId, _current.UserId, ct);
        var turn = await GetAsync(turnId, ct);

        _logger.LogInformation("Turno creado: {Turn} (id {TurnId}) para la caja {CashId} por {Actor}",
            turn.Description, turnId, request.CashId, _current.Username);
        return turn;
    }

    public async Task<TurnDto> UpdateAsync(int id, UpdateTurnRequest request, CancellationToken ct)
    {
        var attention = await FindAsync(id, ct);
        EnsurePending(attention, "mover");
        await EnsureCashCanReceiveAsync(request.CashId, ct);

        attention.Turn.CashId = request.CashId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Turno actualizado: {Turn} movido a la caja {CashId} por {Actor}",
            attention.Turn.Description, request.CashId, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var attention = await FindAsync(id, ct);
        EnsurePending(attention, "eliminar");

        attention.StatusId = AttentionStatusCodes.Cancelled;
        _db.Turns.Remove(attention.Turn); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Turno eliminado (lógico): {Turn} (id {TurnId}) por {Actor}",
            attention.Turn.Description, id, _current.Username);
    }

    // ----- Atención (cajero) -----

    public async Task<TurnDto> StartAsync(int id, CancellationToken ct)
    {
        var attention = await FindAsync(id, ct);
        EnsurePending(attention, "atender");
        await EnsureCashOpenByMeAsync(attention.Turn.CashId, ct);

        var busy = await _db.Attentions.AnyAsync(a =>
            a.UserCashierId == _current.UserId && a.StatusId == AttentionStatusCodes.InProgress, ct);
        if (busy) throw DomainException.Conflict("Ya tiene un turno en atención; finalícelo antes de tomar otro.");

        attention.StatusId = AttentionStatusCodes.InProgress;
        attention.UserCashierId = _current.UserId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Turno {Turn} en atención por {Actor}", attention.Turn.Description, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task<TurnDto> FinishAsync(int id, FinishTurnRequest request, CancellationToken ct)
    {
        var attention = await FindAsync(id, ct);
        if (attention.StatusId != AttentionStatusCodes.InProgress || attention.UserCashierId != _current.UserId)
            throw new DomainException("El turno no está en atención por usted.");
        await EnsureCashOpenByMeAsync(attention.Turn.CashId, ct);

        if (request.ClientId is not null)
        {
            if (!await _db.Clients.AnyAsync(c => c.ClientId == request.ClientId, ct))
                throw DomainException.NotFound("El cliente");
            attention.ClientId = request.ClientId;
        }

        attention.StatusId = AttentionStatusCodes.Attended;
        attention.AttendedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Turno {Turn} atendido por {Actor}", attention.Turn.Description, _current.Username);
        return await GetAsync(id, ct);
    }

    // ----- Reglas compartidas -----

    private static void EnsurePending(Attention attention, string action)
    {
        if (attention.StatusId != AttentionStatusCodes.Pending)
            throw DomainException.Conflict($"Solo se puede {action} un turno pendiente.");
    }

    private async Task EnsureCashCanReceiveAsync(int cashId, CancellationToken ct)
    {
        var cash = await _db.Cashes.Include(c => c.UserCashes).FirstOrDefaultAsync(c => c.CashId == cashId, ct)
            ?? throw DomainException.NotFound("La caja");
        if (!cash.Active) throw new DomainException("La caja está inactiva.");
        if (cash.UserCashes.Count == 0)
            throw new DomainException("La caja no tiene cajeros asignados; asigne uno antes de crear turnos.");
    }

    private async Task EnsureCashOpenByMeAsync(int cashId, CancellationToken ct)
    {
        var openByMe = await _db.Cashes.AnyAsync(c => c.CashId == cashId && c.CurrentUserId == _current.UserId, ct);
        if (!openByMe) throw DomainException.Conflict("Debe abrir la caja antes de atender turnos.");
    }

    private async Task<Attention> FindAsync(int turnId, CancellationToken ct) =>
        await Scoped().FirstOrDefaultAsync(a => a.TurnId == turnId, ct) ?? throw DomainException.NotFound("El turno");
}
