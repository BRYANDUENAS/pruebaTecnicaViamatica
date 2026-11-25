using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Users;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.Gestor)]
public class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users) => _users = users;

    /// <summary>Lista gestores y cajeros. El gestor solo ve los que él creó.</summary>
    [HttpGet]
    public Task<List<UserDto>> List([FromQuery] int? rolId, [FromQuery] string? statusId, CancellationToken ct) =>
        _users.ListAsync(rolId, statusId, ct);

    [HttpGet("{id:int}")]
    public Task<UserDto> Get(int id, CancellationToken ct) => _users.GetAsync(id, ct);

    /// <summary>El administrador crea usuarios activos; los del gestor quedan pendientes de aprobación.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await _users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);
    }

    /// <summary>Carga masiva. El frontend lee el Excel o CSV y envía las filas.</summary>
    [HttpPost("bulk")]
    public Task<BulkResult> Bulk(List<CreateUserRequest> requests, CancellationToken ct) =>
        _users.BulkCreateAsync(requests, ct);

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<UserDto> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        _users.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<UserDto> ChangeStatus(int id, ChangeUserStatusRequest request, CancellationToken ct) =>
        _users.ChangeStatusAsync(id, request, ct);

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = RoleCodes.Admin)]
    public Task<UserDto> Approve(int id, CancellationToken ct) => _users.ApproveAsync(id, ct);

    /// <summary>Eliminación lógica.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleCodes.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _users.DeleteAsync(id, ct);
        return NoContent();
    }
}
