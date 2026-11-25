using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Services;

public record ServiceDto(
    int ServiceId,
    string ServiceName,
    string ServiceDescription,
    int SpeedMbps,
    decimal Price,
    IReadOnlyList<string> Devices);

public record SaveServiceRequest(
    string ServiceName,
    string ServiceDescription,
    int SpeedMbps,
    decimal Price,
    List<string>? Devices);

public class SaveServiceValidator : AbstractValidator<SaveServiceRequest>
{
    public SaveServiceValidator()
    {
        RuleFor(x => x.ServiceName).NotEmpty().WithMessage("El nombre del servicio es obligatorio.").MaximumLength(100);
        RuleFor(x => x.ServiceDescription).NotEmpty().WithMessage("La descripción del servicio es obligatoria.").MaximumLength(150);
        RuleFor(x => x.SpeedMbps).GreaterThan(0).WithMessage("La velocidad debe ser mayor a 0 Mbps.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
        RuleForEach(x => x.Devices).NotEmpty().MaximumLength(50).WithMessage("Cada equipo admite máximo 50 caracteres.");
    }
}

/// <summary>Planes de internet que se pueden contratar, con sus equipos.</summary>
public class InternetServiceService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IValidator<SaveServiceRequest> _validator;
    private readonly ILogger<InternetServiceService> _logger;

    public InternetServiceService(
        IAppDbContext db,
        ICurrentUser current,
        IValidator<SaveServiceRequest> validator,
        ILogger<InternetServiceService> logger)
    {
        _db = db;
        _current = current;
        _validator = validator;
        _logger = logger;
    }

    private static ServiceDto Map(Service s) => new(
        s.ServiceId, s.ServiceName, s.ServiceDescription, s.SpeedMbps, s.Price,
        s.Devices.OrderBy(d => d.DeviceId).Select(d => d.DeviceName).ToList());

    public async Task<List<ServiceDto>> ListAsync(CancellationToken ct)
    {
        var services = await _db.Services.Include(s => s.Devices).OrderBy(s => s.SpeedMbps).ToListAsync(ct);
        return services.Select(Map).ToList();
    }

    public async Task<ServiceDto> GetAsync(int id, CancellationToken ct) => Map(await FindAsync(id, ct));

    public async Task<ServiceDto> CreateAsync(SaveServiceRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var service = new Service();
        Apply(service, request);
        _db.Services.Add(service);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Servicio creado: {Service} (id {ServiceId}) por {Actor}", service.ServiceName, service.ServiceId, _current.Username);
        return Map(service);
    }

    public async Task<ServiceDto> UpdateAsync(int id, SaveServiceRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        var service = await FindAsync(id, ct);
        _db.Devices.RemoveRange(service.Devices);
        service.Devices.Clear();
        Apply(service, request);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Servicio actualizado: id {ServiceId} por {Actor}", id, _current.Username);
        return Map(service);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var service = await FindAsync(id, ct);
        var inUse = await _db.Contracts.AnyAsync(c => c.ServiceId == id && ContractStatusCodes.Open.Contains(c.StatusId), ct);
        if (inUse) throw DomainException.Conflict("El servicio tiene contratos vigentes y no se puede eliminar.");

        _db.Services.Remove(service); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Servicio eliminado (lógico): id {ServiceId} por {Actor}", id, _current.Username);
    }

    private static void Apply(Service service, SaveServiceRequest request)
    {
        service.ServiceName = request.ServiceName.Trim();
        service.ServiceDescription = request.ServiceDescription.Trim();
        service.SpeedMbps = request.SpeedMbps;
        service.Price = request.Price;
        foreach (var name in request.Devices ?? [])
            service.Devices.Add(new Device { DeviceName = name.Trim() });
    }

    private async Task<Service> FindAsync(int id, CancellationToken ct) =>
        await _db.Services.Include(s => s.Devices).FirstOrDefaultAsync(s => s.ServiceId == id, ct)
        ?? throw DomainException.NotFound("El servicio");
}
