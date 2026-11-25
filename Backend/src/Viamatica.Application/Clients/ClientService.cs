using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Users;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Clients;

public class ClientService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IValidator<CreateClientRequest> _createValidator;
    private readonly IValidator<UpdateClientRequest> _updateValidator;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        IAppDbContext db,
        ICurrentUser current,
        IValidator<CreateClientRequest> createValidator,
        IValidator<UpdateClientRequest> updateValidator,
        ILogger<ClientService> logger)
    {
        _db = db;
        _current = current;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <param name="search">Texto a buscar en identificación, nombre o apellido.</param>
    public async Task<List<ClientDto>> ListAsync(string? search, bool? active, CancellationToken ct)
    {
        var query = _db.Clients.AsQueryable();
        if (active is not null) query = query.Where(c => c.Active == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Identification.Contains(term) ||
                c.Name.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term));
        }
        var clients = await query.OrderBy(c => c.LastName).ThenBy(c => c.Name).ToListAsync(ct);
        return clients.Select(ClientDto.From).ToList();
    }

    public async Task<ClientDto> GetAsync(int id, CancellationToken ct) => ClientDto.From(await FindAsync(id, ct));

    /// <summary>Búsqueda exacta por identificación, para saber si el cliente ya está registrado.</summary>
    public async Task<ClientDto> GetByIdentificationAsync(string identification, CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Identification == identification, ct)
            ?? throw DomainException.NotFound($"El cliente con identificación {identification}");
        return ClientDto.From(client);
    }

    public async Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // Un cliente se registra una sola vez; se incluyen los eliminados
        // porque la identificación es única en toda la tabla.
        if (await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.Identification == request.Identification, ct))
            throw DomainException.Conflict($"Ya existe un cliente con la identificación {request.Identification}.");

        var client = new Client
        {
            Name = request.Name.Trim(),
            LastName = request.LastName.Trim(),
            Identification = request.Identification,
            Email = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            Address = request.Address.Trim(),
            ReferenceAddress = request.ReferenceAddress.Trim(),
            Active = true,
            CreationDate = DateTime.UtcNow,
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cliente creado: id {ClientId} por {Actor}", client.ClientId, _current.Username);
        return ClientDto.From(client);
    }

    public async Task<BulkResult> BulkCreateAsync(IReadOnlyList<CreateClientRequest> requests, CancellationToken ct)
    {
        var rows = new List<BulkRowResult>();
        for (var i = 0; i < requests.Count; i++)
        {
            var request = requests[i];
            try
            {
                await CreateAsync(request, ct);
                rows.Add(new BulkRowResult(i + 1, request.Identification, true, null));
            }
            catch (ValidationException ex)
            {
                rows.Add(new BulkRowResult(i + 1, request.Identification, false, string.Join(" ", ex.Errors.Select(e => e.ErrorMessage))));
            }
            catch (DomainException ex)
            {
                rows.Add(new BulkRowResult(i + 1, request.Identification, false, ex.Message));
            }
        }

        var created = rows.Count(r => r.Success);
        _logger.LogInformation("Carga masiva de clientes por {Actor}: {Created} creados, {Failed} con error",
            _current.Username, created, rows.Count - created);
        return new BulkResult(rows.Count, created, rows.Count - created, rows);
    }

    public async Task<ClientDto> UpdateAsync(int id, UpdateClientRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var client = await FindAsync(id, ct);

        client.Name = request.Name.Trim();
        client.LastName = request.LastName.Trim();
        client.Email = request.Email.Trim().ToLowerInvariant();
        client.PhoneNumber = request.PhoneNumber;
        client.Address = request.Address.Trim();
        client.ReferenceAddress = request.ReferenceAddress.Trim();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cliente actualizado: id {ClientId} por {Actor}", id, _current.Username);
        return ClientDto.From(client);
    }

    public async Task<ClientDto> ChangeStatusAsync(int id, ChangeClientStatusRequest request, CancellationToken ct)
    {
        var client = await FindAsync(id, ct);
        client.Active = request.Active;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Estado de cliente actualizado: id {ClientId}, activo {Active}, por {Actor}",
            id, client.Active, _current.Username);
        return ClientDto.From(client);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var client = await FindAsync(id, ct);

        var hasOpenContract = await _db.Contracts
            .AnyAsync(c => c.ClientId == id && ContractStatusCodes.Open.Contains(c.StatusId), ct);
        if (hasOpenContract)
            throw DomainException.Conflict("El cliente tiene un contrato vigente; cancélelo antes de eliminarlo.");

        _db.Clients.Remove(client); // El DbContext lo convierte en eliminación lógica.
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Cliente eliminado (lógico): id {ClientId} por {Actor}", id, _current.Username);
    }

    private async Task<Client> FindAsync(int id, CancellationToken ct) =>
        await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == id, ct) ?? throw DomainException.NotFound("El cliente");
}
