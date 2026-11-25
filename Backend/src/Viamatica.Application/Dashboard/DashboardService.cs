using Microsoft.EntityFrameworkCore;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Common;
using Viamatica.Application.Users;
using Viamatica.Domain.Common;

namespace Viamatica.Application.Dashboard;

/// <summary>Fila que devuelve la función de base de datos <c>fn_cash_indicators</c>.</summary>
public record CashIndicator(int CashId, string CashDescription, int TotalTurns, int Attended, int Pending);

/// <param name="AttendedToday">
/// Turnos atendidos hoy: todos para el administrador, los asignados por él para
/// el gestor y los que atendió para el cajero.
/// </param>
/// <param name="AssignedToday">Turnos creados hoy en el mismo alcance.</param>
/// <param name="PendingUsers">Solo para el gestor: usuarios que creó y esperan aprobación.</param>
public record WelcomeDto(UserDto User, int AttendedToday, int AssignedToday, IReadOnlyList<UserDto> PendingUsers);

public record CountDto(string Key, string Label, int Total);

public record UserIndicatorsDto(
    int Total,
    int SessionActive,
    int SessionInactive,
    int Blocked,
    int PendingApproval,
    IReadOnlyList<CountDto> ByStatus,
    IReadOnlyList<CountDto> ByRole);

public record CashierIndicator(int UserId, string Username, string FullName, string? CashDescription, int Attended);

public record GestorIndicator(int UserId, string Username, string FullName, int Assigned, int Attended, int UsersCreated);

public record OperationIndicatorsDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CashIndicator> Cashes,
    IReadOnlyList<CashierIndicator> Cashiers,
    IReadOnlyList<GestorIndicator> Gestores);

public class DashboardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;

    public DashboardService(IAppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<WelcomeDto> WelcomeAsync(CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.Rol).Include(u => u.Status)
            .FirstOrDefaultAsync(u => u.UserId == _current.UserId, ct)
            ?? throw new DomainException("La sesión ya no es válida.", 401);

        var (from, to) = BusinessDay.RangeUtc(BusinessDay.Today);
        var today = _db.Attentions.Where(a => a.Turn.Date >= from && a.Turn.Date < to);
        if (_current.IsGestor) today = today.Where(a => a.Turn.UserGestorId == _current.UserId);

        var assigned = _current.IsCajero
            ? await today.CountAsync(a => a.Turn.Cash.UserCashes.Any(uc => uc.UserId == _current.UserId), ct)
            : await today.CountAsync(ct);

        var attendedQuery = today.Where(a => a.StatusId == AttentionStatusCodes.Attended);
        if (_current.IsCajero) attendedQuery = attendedQuery.Where(a => a.UserCashierId == _current.UserId);
        var attended = await attendedQuery.CountAsync(ct);

        var pending = new List<UserDto>();
        if (_current.IsGestor)
        {
            var users = await _db.Users.Include(u => u.Rol).Include(u => u.Status)
                .Where(u => u.UserCreate == _current.UserId && u.StatusId == UserStatusCodes.Pending)
                .OrderBy(u => u.CreationDate)
                .ToListAsync(ct);
            pending = users.Select(UserDto.From).ToList();
        }

        return new WelcomeDto(UserDto.From(user), attended, assigned, pending);
    }

    public async Task<UserIndicatorsDto> UsersAsync(CancellationToken ct)
    {
        var users = await _db.Users
            .Select(u => new { u.StatusId, u.RolId, u.SessionActive })
            .ToListAsync(ct);
        var statuses = await _db.UserStatuses.ToListAsync(ct);
        var roles = await _db.Roles.OrderBy(r => r.RolId).ToListAsync(ct);

        return new UserIndicatorsDto(
            users.Count,
            users.Count(u => u.SessionActive),
            users.Count(u => !u.SessionActive),
            users.Count(u => u.StatusId == UserStatusCodes.Blocked),
            users.Count(u => u.StatusId == UserStatusCodes.Pending),
            statuses.Select(s => new CountDto(s.StatusId, s.Description, users.Count(u => u.StatusId == s.StatusId))).ToList(),
            roles.Select(r => new CountDto(r.RolId.ToString(), r.RolName, users.Count(u => u.RolId == r.RolId))).ToList());
    }

    /// <summary>Indicadores de cajas, cajeros y gestores en un rango de fechas (por defecto, hoy).</summary>
    public async Task<OperationIndicatorsDto> OperationsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var start = from ?? BusinessDay.Today;
        var end = to ?? start;
        if (end < start) throw new DomainException("La fecha final no puede ser anterior a la inicial.");
        var (fromUtc, toUtc) = BusinessDay.RangeUtc(start, end);

        var cashes = await _db.CashIndicatorsAsync(fromUtc, toUtc, ct);

        var attentions = await _db.Attentions
            .Where(a => a.Turn.Date >= fromUtc && a.Turn.Date < toUtc)
            .Select(a => new { a.Turn.UserGestorId, a.UserCashierId, a.StatusId })
            .ToListAsync(ct);

        var staff = await _db.Users
            .Where(u => u.RolId == RoleCodes.CajeroId || u.RolId == RoleCodes.GestorId)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                FullName = u.FirstName + " " + u.LastName,
                u.RolId,
                Cash = u.UserCashes.Select(uc => uc.Cash.CashDescription).FirstOrDefault(),
            })
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

        var created = await _db.Users
            .Where(u => u.UserCreate != null && u.CreationDate >= fromUtc && u.CreationDate < toUtc)
            .Select(u => u.UserCreate!.Value)
            .ToListAsync(ct);

        var cashiers = staff.Where(u => u.RolId == RoleCodes.CajeroId)
            .Select(u => new CashierIndicator(u.UserId, u.Username, u.FullName, u.Cash,
                attentions.Count(a => a.UserCashierId == u.UserId && a.StatusId == AttentionStatusCodes.Attended)))
            .ToList();

        var gestores = staff.Where(u => u.RolId == RoleCodes.GestorId)
            .Select(u => new GestorIndicator(u.UserId, u.Username, u.FullName,
                attentions.Count(a => a.UserGestorId == u.UserId),
                attentions.Count(a => a.UserGestorId == u.UserId && a.StatusId == AttentionStatusCodes.Attended),
                created.Count(id => id == u.UserId)))
            .ToList();

        return new OperationIndicatorsDto(start, end, cashes, cashiers, gestores);
    }
}
