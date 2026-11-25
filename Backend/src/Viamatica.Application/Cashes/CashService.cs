using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Cashes;

public record CashUserDto(int UserId, string Username, string FullName);

public record CashDto(
    int CashId,
    string CashDescription,
    bool Active,
    int? CurrentUserId,
    string? CurrentUsername,
    DateTime? OpenedAt,
    IReadOnlyList<CashUserDto> Users);

public record SaveCashRequest(string CashDescription, bool Active = true);

public record AssignCashierRequest(int UserId);

public class SaveCashValidator : AbstractValidator<SaveCashRequest>
{
    public SaveCashValidator()
    {
        RuleFor(x => x.CashDescription)
            .NotEmpty().WithMessage("La descripción de la caja es obligatoria.")
            .MaximumLength(50).WithMessage("La descripción admite máximo 50 caracteres.");
    }
}

public class CashService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IValidator<SaveCashRequest> _validator;
    private readonly ILogger<CashService> _logger;

    public CashService(IAppDbContext db, ICurrentUser current, IValidator<SaveCashRequest> validator, ILogger<CashService> logger)
    {
        _db = db;
        _current = current;
        _validator = validator;
        _logger = logger;
    }

    private IQueryable<Cash> WithDetails() => _db.Cashes
        .Include(c => c.CurrentUser)
        .Include(c => c.UserCashes).ThenInclude(uc => uc.User);

    private static CashDto Map(Cash c) => new(
        c.CashId, c.CashDescription, c.Active, c.CurrentUserId, c.CurrentUser?.Username, c.OpenedAt,
        c.UserCashes
            .OrderBy(uc => uc.User.Username)
            .Select(uc => new CashUserDto(uc.UserId, uc.User.Username, $"{uc.User.FirstName} {uc.User.LastName}"))
            .ToList());

    public async Task<List<CashDto>> ListAsync(CancellationToken ct)
    {
        var cashes = await WithDetails().OrderBy(c => c.CashId).ToListAsync(ct);
        return cashes.Select(Map).ToList();
    }

    public async Task<CashDto> GetAsync(int id, CancellationToken ct) => Map(await FindAsync(id, ct));

    /// <summary>Caja asignada al cajero autenticado, o <c>null</c> si aún no tiene.</summary>
    public async Task<CashDto?> MineAsync(CancellationToken ct)
    {
        var cash = await WithDetails().FirstOrDefaultAsync(c => c.UserCashes.Any(uc => uc.UserId == _current.UserId), ct);
        return cash is null ? null : Map(cash);
    }

    // ----- Mantenimiento (administrador) -----

    public async Task<CashDto> CreateAsync(SaveCashRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var cash = new Cash { CashDescription = request.CashDescription.Trim(), Active = request.Active };
        _db.Cashes.Add(cash);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Caja creada: {Cash} (id {CashId}) por {Actor}", cash.CashDescription, cash.CashId, _current.Username);
        return await GetAsync(cash.CashId, ct);
    }

    public async Task<CashDto> UpdateAsync(int id, SaveCashRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var cash = await FindAsync(id, ct);
        if (!request.Active && cash.CurrentUserId is not null)
            throw DomainException.Conflict("La caja está abierta; debe cerrarse antes de inactivarla.");

        cash.CashDescription = request.CashDescription.Trim();
        cash.Active = request.Active;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Caja actualizada: id {CashId} por {Actor}", id, _current.Username);
        return Map(cash);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var cash = await FindAsync(id, ct);
        if (cash.CurrentUserId is not null)
            throw DomainException.Conflict("La caja está abierta; debe cerrarse antes de eliminarla.");

        _db.UserCashes.RemoveRange(cash.UserCashes);
        _db.Cashes.Remove(cash); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Caja eliminada (lógico): id {CashId} por {Actor}", id, _current.Username);
    }

    // ----- Asignación de cajeros (gestor) -----

    public async Task<CashDto> AssignAsync(int cashId, AssignCashierRequest request, CancellationToken ct)
    {
        var cash = await FindAsync(cashId, ct);
        if (!cash.Active) throw new DomainException("La caja está inactiva.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, ct)
            ?? throw DomainException.NotFound("El usuario");
        if (user.RolId != RoleCodes.CajeroId)
            throw new DomainException("Solo se pueden asignar usuarios cajeros a una caja.");
        if (user.StatusId != UserStatusCodes.Active)
            throw new DomainException("El cajero debe estar activo para asignarlo a una caja.");

        if (cash.UserCashes.Any(uc => uc.UserId == user.UserId))
            throw DomainException.Conflict("El cajero ya está asignado a esta caja.");
        if (cash.UserCashes.Count >= Cash.MaxUsers)
            throw DomainException.Conflict($"La caja ya tiene el máximo de {Cash.MaxUsers} cajeros asignados.");
        if (await _db.UserCashes.AnyAsync(uc => uc.UserId == user.UserId, ct))
            throw DomainException.Conflict("El cajero ya está asignado a otra caja; un cajero solo puede atender una caja.");

        _db.UserCashes.Add(new UserCash
        {
            CashId = cashId,
            UserId = user.UserId,
            AssignedBy = _current.UserId,
            AssignedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cajero {Cashier} asignado a la caja {CashId} por {Actor}", user.Username, cashId, _current.Username);
        return await GetAsync(cashId, ct);
    }

    public async Task<CashDto> UnassignAsync(int cashId, int userId, CancellationToken ct)
    {
        var cash = await FindAsync(cashId, ct);
        var assignment = cash.UserCashes.FirstOrDefault(uc => uc.UserId == userId)
            ?? throw DomainException.NotFound("La asignación");
        if (cash.CurrentUserId == userId)
            throw DomainException.Conflict("El cajero tiene la caja abierta; debe cerrarla antes de retirarlo.");

        _db.UserCashes.Remove(assignment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cajero {UserId} retirado de la caja {CashId} por {Actor}", userId, cashId, _current.Username);
        return await GetAsync(cashId, ct);
    }

    // ----- Apertura y cierre (cajero) -----

    public async Task<CashDto> OpenAsync(CancellationToken ct)
    {
        var cash = await WithDetails().FirstOrDefaultAsync(c => c.UserCashes.Any(uc => uc.UserId == _current.UserId), ct)
            ?? throw new DomainException("No tiene una caja asignada. Solicítela a un gestor.");
        if (!cash.Active) throw new DomainException("La caja está inactiva.");
        if (cash.CurrentUserId == _current.UserId) return Map(cash);

        // Actualización condicional: si dos cajeros intentan abrir a la vez,
        // la base de datos garantiza que solo uno lo consiga.
        var opened = await _db.TryOpenCashAsync(cash.CashId, _current.UserId, ct);

        if (!opened)
        {
            _logger.LogWarning("Apertura rechazada: la caja {CashId} ya está abierta por otro cajero (intento de {Actor})",
                cash.CashId, _current.Username);
            throw DomainException.Conflict("La caja ya está abierta por el otro cajero asignado. Debe cerrarla primero.");
        }

        _logger.LogInformation("Caja {CashId} abierta por {Actor}", cash.CashId, _current.Username);
        // Se relee sin seguimiento: la entidad en memoria quedó desactualizada por el UPDATE directo.
        return Map(await WithDetails().AsNoTracking().FirstAsync(c => c.CashId == cash.CashId, ct));
    }

    public async Task<CashDto> CloseAsync(CancellationToken ct)
    {
        var cash = await WithDetails().FirstOrDefaultAsync(c => c.CurrentUserId == _current.UserId, ct)
            ?? throw new DomainException("No tiene una caja abierta.");

        var inProgress = await _db.Attentions.AnyAsync(a =>
            a.Turn.CashId == cash.CashId && a.StatusId == AttentionStatusCodes.InProgress, ct);
        if (inProgress)
            throw DomainException.Conflict("Hay un turno en atención; finalícelo antes de cerrar la caja.");

        cash.CurrentUserId = null;
        cash.CurrentUser = null;
        cash.OpenedAt = null;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Caja {CashId} cerrada por {Actor}", cash.CashId, _current.Username);
        return Map(cash);
    }

    private async Task<Cash> FindAsync(int id, CancellationToken ct) =>
        await WithDetails().FirstOrDefaultAsync(c => c.CashId == id, ct) ?? throw DomainException.NotFound("La caja");
}
