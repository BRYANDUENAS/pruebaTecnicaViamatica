using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Dashboard;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Datos de la pantalla de bienvenida, según el rol del usuario.</summary>
    [HttpGet("welcome")]
    public Task<WelcomeDto> Welcome(CancellationToken ct) => _dashboard.WelcomeAsync(ct);

    /// <summary>Indicadores de usuarios: sesión activa o inactiva, bloqueados, pendientes.</summary>
    [HttpGet("users")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<UserIndicatorsDto> Users(CancellationToken ct) => _dashboard.UsersAsync(ct);

    /// <summary>Indicadores de cajas, cajeros y gestores en un rango de fechas (por defecto, hoy).</summary>
    [HttpGet("operations")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<OperationIndicatorsDto> Operations([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        _dashboard.OperationsAsync(from, to, ct);
}
