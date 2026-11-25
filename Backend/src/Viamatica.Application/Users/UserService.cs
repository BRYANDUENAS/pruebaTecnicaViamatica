using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Users;

public class UserService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPasswordHasher _hasher;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<ChangeUserStatusRequest> _statusValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAppDbContext db,
        ICurrentUser current,
        IPasswordHasher hasher,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<ChangeUserStatusRequest> statusValidator,
        ILogger<UserService> logger)
    {
        _db = db;
        _current = current;
        _hasher = hasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _logger = logger;
    }

    /// <summary>Gestores y cajeros. El gestor solo ve los usuarios que él creó.</summary>
    private IQueryable<User> Manageable()
    {
        var query = _db.Users
            .Include(u => u.Rol)
            .Include(u => u.Status)
            .Where(u => u.RolId != RoleCodes.AdminId);
        return _current.IsAdmin ? query : query.Where(u => u.UserCreate == _current.UserId);
    }

    public async Task<List<UserDto>> ListAsync(int? rolId, string? statusId, CancellationToken ct)
    {
        var query = Manageable();
        if (rolId is not null) query = query.Where(u => u.RolId == rolId);
        if (!string.IsNullOrWhiteSpace(statusId)) query = query.Where(u => u.StatusId == statusId);
        var users = await query.OrderBy(u => u.Username).ToListAsync(ct);
        return users.Select(UserDto.From).ToList();
    }

    public async Task<UserDto> GetAsync(int id, CancellationToken ct) => UserDto.From(await FindAsync(id, ct));

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == request.Username, ct))
            throw DomainException.Conflict($"El nombre de usuario '{request.Username}' ya existe.");

        // Lo que crea un administrador queda activo; lo que crea un gestor
        // queda pendiente hasta que un administrador lo apruebe.
        var approved = _current.IsAdmin;
        var user = new User
        {
            Username = request.Username,
            Password = _hasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Identification = request.Identification,
            RolId = request.RolId,
            StatusId = approved ? UserStatusCodes.Active : UserStatusCodes.Pending,
            CreationDate = DateTime.UtcNow,
            UserCreate = _current.UserId,
            UserApproval = approved ? _current.UserId : null,
            DateApproval = approved ? DateTime.UtcNow : null,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario creado: {Username} (id {UserId}, rol {RolId}, estado {StatusId}) por {Actor}",
            user.Username, user.UserId, user.RolId, user.StatusId, _current.Username);
        return await GetAsync(user.UserId, ct);
    }

    public async Task<BulkResult> BulkCreateAsync(IReadOnlyList<CreateUserRequest> requests, CancellationToken ct)
    {
        var rows = new List<BulkRowResult>();
        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            try
            {
                await CreateAsync(request, ct);
                rows.Add(new BulkRowResult(i + 1, request.Username, true, null));
            }
            catch (ValidationException ex)
            {
                rows.Add(new BulkRowResult(i + 1, request.Username, false, string.Join(" ", ex.Errors.Select(e => e.ErrorMessage))));
            }
            catch (DomainException ex)
            {
                rows.Add(new BulkRowResult(i + 1, request.Username, false, ex.Message));
            }
        }

        var created = rows.Count(r => r.Success);
        _logger.LogInformation("Carga masiva de usuarios por {Actor}: {Created} creados, {Failed} con error",
            _current.Username, created, rows.Count - created);
        return new BulkResult(rows.Count, created, rows.Count - created, rows);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var user = await FindAsync(id, ct);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.Identification = request.Identification;
        user.RolId = request.RolId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario actualizado: {Username} (id {UserId}) por {Actor}", user.Username, id, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task<UserDto> ChangeStatusAsync(int id, ChangeUserStatusRequest request, CancellationToken ct)
    {
        await _statusValidator.ValidateAndThrowAsync(request, ct);
        var user = await FindAsync(id, ct);
        if (user.StatusId == UserStatusCodes.Pending)
            throw new DomainException("El usuario está pendiente de aprobación; debe aprobarse primero.");

        var previous = user.StatusId;
        user.StatusId = request.StatusId;
        if (request.StatusId == UserStatusCodes.Active) user.FailedAttempts = 0;
        else user.SessionActive = false;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Estado de usuario actualizado: {Username} de {Previous} a {StatusId} por {Actor}",
            user.Username, previous, user.StatusId, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task<UserDto> ApproveAsync(int id, CancellationToken ct)
    {
        var user = await FindAsync(id, ct);
        if (user.StatusId != UserStatusCodes.Pending)
            throw new DomainException("El usuario no está pendiente de aprobación.");

        user.StatusId = UserStatusCodes.Active;
        user.UserApproval = _current.UserId;
        user.DateApproval = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario aprobado: {Username} (id {UserId}) por {Actor}", user.Username, id, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var user = await FindAsync(id, ct);
        user.SessionActive = false;

        // Libera las cajas que el usuario tuviera asignadas o abiertas.
        var assignments = await _db.UserCashes.Where(uc => uc.UserId == id).ToListAsync(ct);
        _db.UserCashes.RemoveRange(assignments);
        var openCashes = await _db.Cashes.Where(c => c.CurrentUserId == id).ToListAsync(ct);
        foreach (var cash in openCashes)
        {
            cash.CurrentUserId = null;
            cash.OpenedAt = null;
        }

        _db.Users.Remove(user); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario eliminado (lógico): {Username} (id {UserId}) por {Actor}", user.Username, id, _current.Username);
    }

    private async Task<User> FindAsync(int id, CancellationToken ct) =>
        await Manageable().FirstOrDefaultAsync(u => u.UserId == id, ct) ?? throw DomainException.NotFound("El usuario");
}
