using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Cashes;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/cashes")]
[Authorize]
public class CashesController : ControllerBase
{
    private const string AdminOrGestor = RoleCodes.Admin + "," + RoleCodes.Gestor;

    private readonly CashService _cashes;

    public CashesController(CashService cashes) => _cashes = cashes;

    [HttpGet]
    [Authorize(Roles = AdminOrGestor)]
    public Task<List<CashDto>> List(CancellationToken ct) => _cashes.ListAsync(ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = AdminOrGestor)]
    public Task<CashDto> Get(int id, CancellationToken ct) => _cashes.GetAsync(id, ct);

    // ----- Mantenimiento: solo administrador -----

    [HttpPost]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Create(SaveCashRequest request, CancellationToken ct)
    {
        var cash = await _cashes.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = cash.CashId }, cash);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<CashDto> Update(int id, SaveCashRequest request, CancellationToken ct) => _cashes.UpdateAsync(id, request, ct);

    /// <summary>Eliminación lógica.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _cashes.DeleteAsync(id, ct);
        return NoContent();
    }

    // ----- Asignación de cajeros: solo gestor -----

    /// <summary>Asigna un cajero a la caja. Máximo 2 por caja y una sola caja por cajero.</summary>
    [HttpPost("{id:int}/users")]
    [Authorize(Roles = RoleCodes.Gestor)]
    public Task<CashDto> Assign(int id, AssignCashierRequest request, CancellationToken ct) => _cashes.AssignAsync(id, request, ct);

    [HttpDelete("{id:int}/users/{userId:int}")]
    [Authorize(Roles = RoleCodes.Gestor)]
    public Task<CashDto> Unassign(int id, int userId, CancellationToken ct) => _cashes.UnassignAsync(id, userId, ct);

    // ----- Apertura y cierre: solo cajero -----

    /// <summary>Caja asignada al cajero autenticado.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = RoleCodes.Cajero)]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var cash = await _cashes.MineAsync(ct);
        return cash is null ? NoContent() : Ok(cash);
    }

    /// <summary>Abre la caja. Falla si el otro cajero asignado la tiene abierta.</summary>
    [HttpPost("mine/open")]
    [Authorize(Roles = RoleCodes.Cajero)]
    public Task<CashDto> Open(CancellationToken ct) => _cashes.OpenAsync(ct);

    [HttpPost("mine/close")]
    [Authorize(Roles = RoleCodes.Cajero)]
    public Task<CashDto> Close(CancellationToken ct) => _cashes.CloseAsync(ct);
}
