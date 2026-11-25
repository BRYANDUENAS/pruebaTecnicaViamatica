using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Common;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Contracts;

public record ContractDto(
    int ContractId,
    DateTime StartDate,
    DateTime EndDate,
    int ClientId,
    string ClientName,
    string ClientIdentification,
    int ServiceId,
    string ServiceName,
    int SpeedMbps,
    decimal Price,
    string StatusId,
    string StatusDescription,
    int MethodPaymentId,
    string MethodPaymentDescription,
    int? PreviousContractId);

public record CreateContractRequest(int ClientId, int ServiceId, int MethodPaymentId);

public record ChangeServiceRequest(int ServiceId);

public record ChangeMethodPaymentRequest(int MethodPaymentId);

public class ContractService
{
    /// <summary>El enunciado no fija la duración; se definió 1 año por defecto.</summary>
    public const int DefaultDurationYears = 1;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ILogger<ContractService> _logger;

    public ContractService(IAppDbContext db, ICurrentUser current, ILogger<ContractService> logger)
    {
        _db = db;
        _current = current;
        _logger = logger;
    }

    // Se ignoran los filtros globales para que el historial siga visible aunque el
    // servicio o el cliente se hayan eliminado lógicamente; el contrato sí se filtra.
    private IQueryable<Contract> WithDetails() => _db.Contracts
        .IgnoreQueryFilters()
        .Where(c => !c.IsDeleted)
        .Include(c => c.Client)
        .Include(c => c.Service)
        .Include(c => c.Status)
        .Include(c => c.MethodPayment);

    private static ContractDto Map(Contract c) => new(
        c.ContractId, c.StartDate, c.EndDate, c.ClientId, $"{c.Client.Name} {c.Client.LastName}",
        c.Client.Identification, c.ServiceId, c.Service.ServiceName, c.Service.SpeedMbps, c.Service.Price,
        c.StatusId, c.Status.Description, c.MethodPaymentId, c.MethodPayment.Description, c.PreviousContractId);

    public async Task<List<ContractDto>> ListAsync(int? clientId, string? statusId, CancellationToken ct)
    {
        var query = WithDetails();
        if (clientId is not null) query = query.Where(c => c.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(statusId)) query = query.Where(c => c.StatusId == statusId);
        var contracts = await query.OrderByDescending(c => c.ContractId).ToListAsync(ct);
        return contracts.Select(Map).ToList();
    }

    public async Task<ContractDto> GetAsync(int id, CancellationToken ct) => Map(await FindAsync(id, ct));

    /// <summary>Contratación de un servicio para un cliente ya registrado.</summary>
    public async Task<ContractDto> CreateAsync(CreateContractRequest request, CancellationToken ct)
    {
        await _db.EnsureCashOpenAsync(_current, ct);

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == request.ClientId, ct)
            ?? throw DomainException.NotFound("El cliente");
        if (!client.Active) throw new DomainException("El cliente está inactivo.");
        await EnsureServiceExistsAsync(request.ServiceId, ct);
        await EnsureMethodPaymentExistsAsync(request.MethodPaymentId, ct);

        var hasOpen = await _db.Contracts.AnyAsync(c =>
            c.ClientId == request.ClientId && ContractStatusCodes.Open.Contains(c.StatusId), ct);
        if (hasOpen)
            throw DomainException.Conflict("El cliente ya tiene un contrato vigente. Use el cambio de servicio o cancélelo primero.");

        var now = DateTime.UtcNow;
        var contract = new Contract
        {
            ClientId = request.ClientId,
            ServiceId = request.ServiceId,
            MethodPaymentId = request.MethodPaymentId,
            StatusId = ContractStatusCodes.Vigente,
            StartDate = now,
            EndDate = now.AddYears(DefaultDurationYears),
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contrato creado: id {ContractId}, cliente {ClientId}, servicio {ServiceId}, por {Actor}",
            contract.ContractId, contract.ClientId, contract.ServiceId, _current.Username);
        return await GetAsync(contract.ContractId, ct);
    }

    /// <summary>
    /// Cambio de servicio: el contrato actual pasa a "sustituido" y se crea uno
    /// nuevo en estado "renovación de servicio" que conserva la fecha final.
    /// </summary>
    public async Task<ContractDto> ChangeServiceAsync(int id, ChangeServiceRequest request, CancellationToken ct)
    {
        await _db.EnsureCashOpenAsync(_current, ct);
        var current = await FindOpenAsync(id, ct);
        if (current.ServiceId == request.ServiceId)
            throw new DomainException("El cliente ya tiene contratado ese servicio; elija uno de mayor o menor velocidad.");
        await EnsureServiceExistsAsync(request.ServiceId, ct);

        current.StatusId = ContractStatusCodes.Sustituido;
        var renewed = new Contract
        {
            ClientId = current.ClientId,
            ServiceId = request.ServiceId,
            MethodPaymentId = current.MethodPaymentId,
            StatusId = ContractStatusCodes.Renovado,
            StartDate = DateTime.UtcNow,
            EndDate = current.EndDate,
            PreviousContractId = current.ContractId,
        };
        _db.Contracts.Add(renewed);
        await _db.SaveChangesAsync(ct); // Ambos cambios se guardan en una sola transacción.

        _logger.LogInformation("Cambio de servicio: contrato {OldId} sustituido por {NewId} (servicio {ServiceId}) por {Actor}",
            current.ContractId, renewed.ContractId, renewed.ServiceId, _current.Username);
        return await GetAsync(renewed.ContractId, ct);
    }

    public async Task<ContractDto> ChangeMethodPaymentAsync(int id, ChangeMethodPaymentRequest request, CancellationToken ct)
    {
        await _db.EnsureCashOpenAsync(_current, ct);
        var contract = await FindOpenAsync(id, ct);
        if (contract.MethodPaymentId == request.MethodPaymentId)
            throw new DomainException("El contrato ya tiene esa forma de pago.");
        await EnsureMethodPaymentExistsAsync(request.MethodPaymentId, ct);

        var previous = contract.MethodPaymentId;
        contract.MethodPaymentId = request.MethodPaymentId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Forma de pago actualizada: contrato {ContractId} de {Previous} a {MethodPaymentId} por {Actor}",
            id, previous, request.MethodPaymentId, _current.Username);
        return await GetAsync(id, ct);
    }

    /// <summary>Cancelación: cambia el estado y la fecha fin pasa a ser la de la cancelación.</summary>
    public async Task<ContractDto> CancelAsync(int id, CancellationToken ct)
    {
        await _db.EnsureCashOpenAsync(_current, ct);
        var contract = await FindOpenAsync(id, ct);

        contract.StatusId = ContractStatusCodes.Cancelado;
        contract.EndDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contrato cancelado: id {ContractId} por {Actor}", id, _current.Username);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var contract = await FindAsync(id, ct);
        if (ContractStatusCodes.Open.Contains(contract.StatusId))
            throw DomainException.Conflict("El contrato está vigente; cancélelo antes de eliminarlo.");

        _db.Contracts.Remove(contract); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contrato eliminado (lógico): id {ContractId} por {Actor}", id, _current.Username);
    }

    private async Task<Contract> FindAsync(int id, CancellationToken ct) =>
        await WithDetails().FirstOrDefaultAsync(c => c.ContractId == id, ct) ?? throw DomainException.NotFound("El contrato");

    private async Task<Contract> FindOpenAsync(int id, CancellationToken ct)
    {
        var contract = await FindAsync(id, ct);
        if (!ContractStatusCodes.Open.Contains(contract.StatusId))
            throw DomainException.Conflict($"El contrato no está vigente (estado: {contract.Status.Description}).");
        return contract;
    }

    private async Task EnsureServiceExistsAsync(int serviceId, CancellationToken ct)
    {
        if (!await _db.Services.AnyAsync(s => s.ServiceId == serviceId, ct))
            throw DomainException.NotFound("El servicio");
    }

    private async Task EnsureMethodPaymentExistsAsync(int methodPaymentId, CancellationToken ct)
    {
        if (!await _db.MethodPayments.AnyAsync(m => m.MethodPaymentId == methodPaymentId, ct))
            throw DomainException.NotFound("La forma de pago");
    }
}
