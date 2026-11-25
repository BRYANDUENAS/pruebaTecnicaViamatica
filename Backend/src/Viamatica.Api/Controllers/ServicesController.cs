using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Services;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

/// <summary>Planes de internet. Todos los consultan; solo el administrador los mantiene.</summary>
[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly InternetServiceService _services;

    public ServicesController(InternetServiceService services) => _services = services;

    [HttpGet]
    public Task<List<ServiceDto>> List(CancellationToken ct) => _services.ListAsync(ct);

    [HttpGet("{id:int}")]
    public Task<ServiceDto> Get(int id, CancellationToken ct) => _services.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Create(SaveServiceRequest request, CancellationToken ct)
    {
        var service = await _services.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = service.ServiceId }, service);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<ServiceDto> Update(int id, SaveServiceRequest request, CancellationToken ct) => _services.UpdateAsync(id, request, ct);

    /// <summary>Eliminación lógica. No se permite si hay contratos vigentes con el servicio.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _services.DeleteAsync(id, ct);
        return NoContent();
    }
}
