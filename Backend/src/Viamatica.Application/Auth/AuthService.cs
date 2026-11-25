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

/// <summary>Recuperación por correo: se envía una contraseña temporal al correo registrado.</summary>
public record ForgotPasswordRequest(string Username, string Email);

public record RecoveryOptions(bool EmailEnabled);

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("El nombre de usuario es obligatorio.").MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El correo no es válido.");
    }
}

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
    private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext db,
        ICurrentUser current,
        IPasswordHasher hasher,
        ITokenService tokens,
        IValidator<LoginRequest> loginValidator,
        IValidator<RecoverPasswordRequest> recoverValidator,
        IValidator<ForgotPasswordRequest> forgotValidator,
        IEmailSender email,
        ILogger<AuthService> logger)
    {
        _db = db;
        _current = current;
        _hasher = hasher;
        _tokens = tokens;
        _loginValidator = loginValidator;
        _recoverValidator = recoverValidator;
        _forgotValidator = forgotValidator;
        _email = email;
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

    public RecoveryOptions RecoveryOptions() => new(_email.IsConfigured);

    /// <summary>
    /// Genera una contraseña temporal y la envía al correo registrado. La respuesta
    /// es la misma exista o no el usuario, para no revelar qué cuentas existen.
    /// </summary>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _forgotValidator.ValidateAndThrowAsync(request, ct);
        if (!_email.IsConfigured)
            throw new DomainException("El envío de correos no está configurado. Use la validación de identidad.", 503);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.Email == email, ct);
        if (user is null || user.StatusId is UserStatusCodes.Pending or UserStatusCodes.Inactive)
        {
            _logger.LogWarning("Recuperación por correo sin efecto para {Username}: datos incorrectos o usuario no habilitado", request.Username);
            return;
        }

        // Primero se envía el correo: si falla, la contraseña actual no cambia.
        var temporary = TemporaryPassword();
        await _email.SendAsync(email, "NetCaja: su contraseña temporal", $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#1a1c1e">
              <h2 style="color:#0b57b0">Recuperación de contraseña</h2>
              <p>Hola, {System.Net.WebUtility.HtmlEncode(user.FirstName)}:</p>
              <p>Se solicitó recuperar la contraseña del usuario <strong>{user.Username}</strong>. Su contraseña temporal es:</p>
              <p style="font-size:22px;font-family:monospace;letter-spacing:2px;background:#e0f5f5;padding:14px;border-radius:8px;text-align:center">{temporary}</p>
              <p>Ingrese al sistema con ella. Si usted no hizo esta solicitud, comuníquese con un administrador.</p>
            </div>
            """, ct);

        user.Password = _hasher.Hash(temporary);
        user.FailedAttempts = 0;
        user.SessionActive = false;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Contraseña temporal enviada por correo al usuario {Username}", user.Username);
    }

    /// <summary>12 caracteres aleatorios, con mayúscula y número garantizados; sin caracteres ambiguos.</summary>
    private static string TemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789";
        var all = upper + lower + digits;
        var chars = new List<char>
        {
            upper[System.Security.Cryptography.RandomNumberGenerator.GetInt32(upper.Length)],
            digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)],
        };
        while (chars.Count < 12) chars.Add(all[System.Security.Cryptography.RandomNumberGenerator.GetInt32(all.Length)]);
        System.Security.Cryptography.RandomNumberGenerator.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(chars));
        return new string(chars.ToArray());
    }

    private async Task<IReadOnlyList<MenuItemDto>> MenuAsync(int rolId, CancellationToken ct) =>
        await _db.RolMenus
            .Where(rm => rm.RolId == rolId)
            .OrderBy(rm => rm.MenuItem.Order)
            .Select(rm => new MenuItemDto(rm.MenuItem.MenuItemId, rm.MenuItem.Label, rm.MenuItem.Route, rm.MenuItem.Icon))
            .ToListAsync(ct);
}
