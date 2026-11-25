using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Payments;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = RoleCodes.Cajero)]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _payments;

    public PaymentsController(PaymentService payments) => _payments = payments;

    [HttpGet]
    public Task<List<PaymentDto>> List([FromQuery] int? clientId, [FromQuery] int? contractId, CancellationToken ct) =>
        _payments.ListAsync(clientId, contractId, ct);

    [HttpGet("{id:int}")]
    public Task<PaymentDto> Get(int id, CancellationToken ct) => _payments.GetAsync(id, ct);

    /// <summary>Registra el pago de un contrato vigente por el precio del servicio.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentRequest request, CancellationToken ct)
    {
        var payment = await _payments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = payment.PaymentId }, payment);
    }

    /// <summary>Corrige la forma de pago de un pago registrado.</summary>
    [HttpPut("{id:int}")]
    public Task<PaymentDto> Update(int id, UpdatePaymentRequest request, CancellationToken ct) =>
        _payments.UpdateAsync(id, request, ct);

    /// <summary>Anula el pago (eliminación lógica).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _payments.DeleteAsync(id, ct);
        return NoContent();
    }
}
