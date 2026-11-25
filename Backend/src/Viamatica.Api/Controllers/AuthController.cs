using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Auth;
using Viamatica.Application.Users;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Inicia sesión y devuelve el JWT, los datos del usuario y su menú.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => _auth.LoginAsync(request, ct);

    /// <summary>Define una nueva contraseña validando usuario, correo e identificación.</summary>
    [HttpPost("recover-password")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverPassword(RecoverPasswordRequest request, CancellationToken ct)
    {
        await _auth.RecoverPasswordAsync(request, ct);
        return Ok(new { message = "La contraseña se actualizó correctamente." });
    }

    /// <summary>Indica al frontend si la recuperación por correo está disponible.</summary>
    [HttpGet("recovery-options")]
    [AllowAnonymous]
    public RecoveryOptions RecoveryOptions() => _auth.RecoveryOptions();

    /// <summary>Envía una contraseña temporal al correo registrado del usuario.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request, ct);
        return Ok(new { message = "Si los datos son correctos, enviamos una contraseña temporal al correo registrado." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public Task<UserDto> Me(CancellationToken ct) => _auth.MeAsync(ct);

    /// <summary>Menú del rol del usuario autenticado, leído de la base de datos.</summary>
    [HttpGet("menu")]
    [Authorize]
    public Task<IReadOnlyList<MenuItemDto>> Menu(CancellationToken ct) => _auth.MyMenuAsync(ct);
}
