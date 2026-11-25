using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Contracts;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

/// <summary>Procesos de caja sobre contratos. Requieren que el cajero tenga la caja abierta.</summary>
[ApiController]
[Route("api/contracts")]
[Authorize(Roles = RoleCodes.Cajero)]
public class ContractsController : ControllerBase
{
    private readonly ContractService _contracts;

    public ContractsController(ContractService contracts) => _contracts = contracts;

    [HttpGet]
    public Task<List<ContractDto>> List([FromQuery] int? clientId, [FromQuery] string? statusId, CancellationToken ct) =>
        _contracts.ListAsync(clientId, statusId, ct);

    [HttpGet("{id:int}")]
    public Task<ContractDto> Get(int id, CancellationToken ct) => _contracts.GetAsync(id, ct);

    /// <summary>Contrata un servicio. Dura 1 año y solo se permite un contrato vigente por cliente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateContractRequest request, CancellationToken ct)
    {
        var contract = await _contracts.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = contract.ContractId }, contract);
    }

    /// <summary>El contrato actual queda sustituido y se crea uno de renovación con la misma fecha final.</summary>
    [HttpPost("{id:int}/change-service")]
    public Task<ContractDto> ChangeService(int id, ChangeServiceRequest request, CancellationToken ct) =>
        _contracts.ChangeServiceAsync(id, request, ct);

    [HttpPut("{id:int}/method-payment")]
    public Task<ContractDto> ChangeMethodPayment(int id, ChangeMethodPaymentRequest request, CancellationToken ct) =>
        _contracts.ChangeMethodPaymentAsync(id, request, ct);

    /// <summary>Cancela el contrato: actualiza el estado y la fecha fin.</summary>
    [HttpPost("{id:int}/cancel")]
    public Task<ContractDto> Cancel(int id, CancellationToken ct) => _contracts.CancelAsync(id, ct);

    /// <summary>Eliminación lógica. Solo para contratos que ya no están vigentes.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _contracts.DeleteAsync(id, ct);
        return NoContent();
    }
}
