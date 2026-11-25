using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Clients;
using Viamatica.Application.Users;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = RoleCodes.Cajero)]
public class ClientsController : ControllerBase
{
    private readonly ClientService _clients;

    public ClientsController(ClientService clients) => _clients = clients;

    /// <summary>Lista clientes. <c>search</c> busca en identificación, nombre o apellido.</summary>
    [HttpGet]
    public Task<List<ClientDto>> List([FromQuery] string? search, [FromQuery] bool? active, CancellationToken ct) =>
        _clients.ListAsync(search, active, ct);

    [HttpGet("{id:int}")]
    public Task<ClientDto> Get(int id, CancellationToken ct) => _clients.GetAsync(id, ct);

    /// <summary>Busca un cliente ya registrado por su número de identificación.</summary>
    [HttpGet("by-identification/{identification}")]
    public Task<ClientDto> GetByIdentification(string identification, CancellationToken ct) =>
        _clients.GetByIdentificationAsync(identification, ct);

    [HttpPost]
    public async Task<IActionResult> Create(CreateClientRequest request, CancellationToken ct)
    {
        var client = await _clients.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = client.ClientId }, client);
    }

    /// <summary>Carga masiva. El frontend lee el Excel o CSV y envía las filas.</summary>
    [HttpPost("bulk")]
    public Task<BulkResult> Bulk(List<CreateClientRequest> requests, CancellationToken ct) =>
        _clients.BulkCreateAsync(requests, ct);

    [HttpPut("{id:int}")]
    public Task<ClientDto> Update(int id, UpdateClientRequest request, CancellationToken ct) =>
        _clients.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    public Task<ClientDto> ChangeStatus(int id, ChangeClientStatusRequest request, CancellationToken ct) =>
        _clients.ChangeStatusAsync(id, request, ct);

    /// <summary>Eliminación lógica. No se permite si el cliente tiene un contrato vigente.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _clients.DeleteAsync(id, ct);
        return NoContent();
    }
}
