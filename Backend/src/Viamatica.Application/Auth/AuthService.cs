using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Users;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Auth;

public record LoginRequest(string Username, string Password);

public record MenuItemDto(int MenuItemId, string Label, string Route, string Icon);

public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User, IReadOnlyList<MenuItemDto> Menu);

/// <summary>
/// Recuperación sin servidor de correo: el usuario demuestra su identidad con
/// usuario, correo e identificación, y define una nueva contraseña.
/// </summary>
public record RecoverPasswordRequest(string Username, string Email, string Identification, string NewPassword);

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("El nombre de usuario es obligatorio.").MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria.").MaximumLength(30);
    }
}

public class RecoverPasswordValidator : AbstractValidator<RecoverPasswordRequest>
{
    public RecoverPasswordValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Identification).ValidIdentification();
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}

public class AuthService
{
    public const int MaxFailedAttempts = 3;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RecoverPasswordRequest> _recoverValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext db,
        ICurrentUser current,
        IPasswordHasher hasher,
        ITokenService tokens,
        IValidator<LoginRequest> loginValidator,
        IValidator<RecoverPasswordRequest> recoverValidator,
        ILogger<AuthService> logger)
    {
        _db = db;
        _current = current;
        _hasher = hasher;
        _tokens = tokens;
        _loginValidator = loginValidator;
        _recoverValidator = recoverValidator;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);

        var user = await _db.Users
            .Include(u => u.Rol)
            .Include(u => u.Status)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null)
        {
            _logger.LogWarning("Inicio de sesión fallido: el usuario {Username} no existe", request.Username);
            throw new DomainException("Usuario o contraseña incorrectos.", 401);
        }

        switch (user.StatusId)
        {
            case UserStatusCodes.Blocked:
                _logger.LogWarning("Inicio de sesión rechazado: {Username} está bloqueado", user.Username);
                throw DomainException.Forbidden("El usuario está bloqueado. Contacte a un administrador.");
            case UserStatusCodes.Pending:
                _logger.LogWarning("Inicio de sesión rechazado: {Username} está pendiente de aprobación", user.Username);
                throw DomainException.Forbidden("El usuario está pendiente de aprobación por un administrador.");
            case UserStatusCodes.Inactive:
                _logger.LogWarning("Inicio de sesión rechazado: {Username} está inactivo", user.Username);
                throw DomainException.Forbidden("El usuario está inactivo.");
        }

        if (!_hasher.Verify(request.Password, user.Password))
        {
            user.FailedAttempts++;
            var blocked = user.FailedAttempts >= MaxFailedAttempts;
            if (blocked) user.StatusId = UserStatusCodes.Blocked;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("Inicio de sesión fallido: contraseña incorrecta para {Username} (intento {Attempt} de {Max})",
                user.Username, user.FailedAttempts, MaxFailedAttempts);
            if (blocked)
            {
                _logger.LogWarning("Usuario {Username} bloqueado por intentos fallidos", user.Username);
                throw DomainException.Forbidden("El usuario fue bloqueado por superar los intentos permitidos.");
            }
            throw new DomainException("Usuario o contraseña incorrectos.", 401);
        }

        user.FailedAttempts = 0;
        user.SessionActive = true;
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokens.Create(user);
        _logger.LogInformation("Inicio de sesión: {Username} (rol {Rol})", user.Username, user.Rol.RolName);
        return new LoginResponse(token, expiresAt, UserDto.From(user), await MenuAsync(user.RolId, ct));
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == _current.UserId, ct);
        if (user is null) return;

        user.SessionActive = false;

        // Al cerrar sesión se libera la caja para que el otro cajero pueda usarla.
        var cashes = await _db.Cashes.Where(c => c.CurrentUserId == user.UserId).ToListAsync(ct);
        foreach (var cash in cashes)
        {
            cash.CurrentUserId = null;
            cash.OpenedAt = null;
        }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cierre de sesión: {Username}", user.Username);
    }

    public async Task<UserDto> MeAsync(CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Rol)
            .Include(u => u.Status)
            .FirstOrDefaultAsync(u => u.UserId == _current.UserId, ct)
            ?? throw new DomainException("La sesión ya no es válida.", 401);
        return UserDto.From(user);
    }

    public Task<IReadOnlyList<MenuItemDto>> MyMenuAsync(CancellationToken ct) => MenuAsync(_current.RolId, ct);

    public async Task RecoverPasswordAsync(RecoverPasswordRequest request, CancellationToken ct)
    {
        await _recoverValidator.ValidateAndThrowAsync(request, ct);

        // Correo e identificación están cifrados de forma determinista, así que
        // EF Core compara los valores cifrados sin exponer el dato.
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == request.Username &&
            u.Email == email &&
            u.Identification == request.Identification, ct);

        if (user is null)
        {
            _logger.LogWarning("Recuperación de contraseña fallida para {Username}: los datos no coinciden", request.Username);
            throw new DomainException("Los datos ingresados no coinciden con ningún usuario.");
        }
        if (user.StatusId is UserStatusCodes.Pending or UserStatusCodes.Inactive)
            throw DomainException.Forbidden("El usuario no está habilitado para recuperar la contraseña.");

        user.Password = _hasher.Hash(request.NewPassword);
        user.FailedAttempts = 0;
        user.SessionActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Contraseña recuperada por el usuario {Username}", user.Username);
    }

    private async Task<IReadOnlyList<MenuItemDto>> MenuAsync(int rolId, CancellationToken ct) =>
        await _db.RolMenus
            .Where(rm => rm.RolId == rolId)
            .OrderBy(rm => rm.MenuItem.Order)
            .Select(rm => new MenuItemDto(rm.MenuItem.MenuItemId, rm.MenuItem.Label, rm.MenuItem.Route, rm.MenuItem.Icon))
            .ToListAsync(ct);
}
