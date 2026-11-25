using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Common;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Payments;

public record PaymentDto(
    int PaymentId,
    DateTime PaymentDate,
    decimal Amount,
    int ClientId,
    string ClientName,
    int ContractId,
    string ServiceName,
    int MethodPaymentId,
    string MethodPaymentDescription,
    int UserCashierId);

/// <summary>Si no se indica forma de pago, se usa la del contrato.</summary>
public record CreatePaymentRequest(int ContractId, int? MethodPaymentId);

public record UpdatePaymentRequest(int MethodPaymentId);

public class PaymentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IAppDbContext db, ICurrentUser current, ILogger<PaymentService> logger)
    {
        _db = db;
        _current = current;
        _logger = logger;
    }

    // Se ignoran los filtros globales para que un pago siga visible aunque su
    // contrato o su cliente se hayan eliminado lógicamente; el pago sí se filtra.
    private IQueryable<Payment> WithDetails() => _db.Payments
        .IgnoreQueryFilters()
        .Where(p => !p.IsDeleted)
        .Include(p => p.Client)
        .Include(p => p.Contract).ThenInclude(c => c.Service)
        .Include(p => p.MethodPayment);

    private static PaymentDto Map(Payment p) => new(
        p.PaymentId, p.PaymentDate, p.Amount, p.ClientId, $"{p.Client.Name} {p.Client.LastName}",
        p.ContractId, p.Contract.Service.ServiceName, p.MethodPaymentId, p.MethodPayment.Description, p.UserCashierId);

    public async Task<List<PaymentDto>> ListAsync(int? clientId, int? contractId, CancellationToken ct)
    {
        var query = WithDetails();
        if (clientId is not null) query = query.Where(p => p.ClientId == clientId);
        if (contractId is not null) query = query.Where(p => p.ContractId == contractId);
        var payments = await query.OrderByDescending(p => p.PaymentId).ToListAsync(ct);
        return payments.Select(Map).ToList();
    }

    public async Task<PaymentDto> GetAsync(int id, CancellationToken ct) => Map(await FindAsync(id, ct));

    /// <summary>Registra el pago mensual. El monto es siempre el precio del servicio contratado.</summary>
    public async Task<PaymentDto> CreateAsync(CreatePaymentRequest request, CancellationToken ct)
    {
        await _db.EnsureCashOpenAsync(_current, ct);

        var contract = await _db.Contracts.Include(c => c.Service)
            .FirstOrDefaultAsync(c => c.ContractId == request.ContractId, ct)
            ?? throw DomainException.NotFound("El contrato");
        if (!ContractStatusCodes.Open.Contains(contract.StatusId))
            throw DomainException.Conflict("Solo se registran pagos de contratos vigentes.");

        var methodPaymentId = request.MethodPaymentId ?? contract.MethodPaymentId;
        await EnsureMethodPaymentExistsAsync(methodPaymentId, ct);

        var payment = new Payment
        {
            ContractId = contract.ContractId,
            ClientId = contract.ClientId,
            Amount = contract.Service.Price,
            MethodPaymentId = methodPaymentId,
            PaymentDate = DateTime.UtcNow,
            UserCashierId = _current.UserId,
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Pago registrado: id {PaymentId}, contrato {ContractId}, monto {Amount}, por {Actor}",
            payment.PaymentId, payment.ContractId, payment.Amount, _current.Username);
        return await GetAsync(payment.PaymentId, ct);
    }

    /// <summary>Solo se corrige la forma de pago; el monto y el contrato no cambian.</summary>
    public async Task<PaymentDto> UpdateAsync(int id, UpdatePaymentRequest request, CancellationToken ct)
    {
        var payment = await FindAsync(id, ct);
        await EnsureMethodPaymentExistsAsync(request.MethodPaymentId, ct);

        payment.MethodPaymentId = request.MethodPaymentId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Pago actualizado: id {PaymentId} por {Actor}", id, _current.Username);
        return await GetAsync(id, ct);
    }

    /// <summary>Anulación del pago mediante eliminación lógica.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var payment = await FindAsync(id, ct);
        _db.Payments.Remove(payment); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Pago anulado (eliminación lógica): id {PaymentId} por {Actor}", id, _current.Username);
    }

    private async Task<Payment> FindAsync(int id, CancellationToken ct) =>
        await WithDetails().FirstOrDefaultAsync(p => p.PaymentId == id, ct) ?? throw DomainException.NotFound("El pago");

    private async Task EnsureMethodPaymentExistsAsync(int methodPaymentId, CancellationToken ct)
    {
        if (!await _db.MethodPayments.AnyAsync(m => m.MethodPaymentId == methodPaymentId, ct))
            throw DomainException.NotFound("La forma de pago");
    }
}
