using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Turns;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/turns")]
[Authorize]
public class TurnsController : ControllerBase
{
    private readonly TurnService _turns;

    public TurnsController(TurnService turns) => _turns = turns;

    /// <summary>
    /// Turnos de un día (por defecto hoy). El administrador ve todos, el gestor
    /// los que asignó y el cajero los de su caja.
    /// </summary>
    [HttpGet]
    public Task<List<TurnDto>> List([FromQuery] DateOnly? date, [FromQuery] int? cashId, [FromQuery] int? statusId, CancellationToken ct) =>
        _turns.ListAsync(date, cashId, statusId, ct);

    [HttpGet("{id:int}")]
    public Task<TurnDto> Get(int id, CancellationToken ct) => _turns.GetAsync(id, ct);

    // ----- Asignación: solo gestor -----

    /// <summary>Crea un turno para una caja. La descripción la genera la función fn_create_turn.</summary>
    [HttpPost]
    [Authorize(Roles = RoleCodes.Gestor)]
    public async Task<IActionResult> Create(CreateTurnRequest request, CancellationToken ct)
    {
        var turn = await _turns.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = turn.TurnId }, turn);
    }

    /// <summary>Mueve un turno pendiente a otra caja.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleCodes.Gestor)]
    public Task<TurnDto> Update(int id, UpdateTurnRequest request, CancellationToken ct) => _turns.UpdateAsync(id, request, ct);

    /// <summary>Eliminación lógica de un turno pendiente.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleCodes.Gestor)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _turns.DeleteAsync(id, ct);
        return NoContent();
    }

    // ----- Atención: solo cajero, con la caja abierta -----

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = RoleCodes.Cajero)]
    public Task<TurnDto> Start(int id, CancellationToken ct) => _turns.StartAsync(id, ct);

    [HttpPost("{id:int}/finish")]
    [Authorize(Roles = RoleCodes.Cajero)]
    public Task<TurnDto> Finish(int id, FinishTurnRequest request, CancellationToken ct) => _turns.FinishAsync(id, request, ct);
}
