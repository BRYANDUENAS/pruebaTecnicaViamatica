using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viamatica.Application.Abstractions;

namespace Viamatica.Api.Controllers;

/// <summary>Catálogos de solo lectura para llenar listas en el frontend.</summary>
[ApiController]
[Route("api/catalogs")]
[Authorize]
public class CatalogsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public CatalogsController(IAppDbContext db) => _db = db;

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) =>
        Ok(await _db.Roles.OrderBy(r => r.RolId).Select(r => new { r.RolId, r.RolName }).ToListAsync(ct));

    [HttpGet("user-statuses")]
    public async Task<IActionResult> UserStatuses(CancellationToken ct) =>
        Ok(await _db.UserStatuses.Select(s => new { s.StatusId, s.Description }).ToListAsync(ct));

    [HttpGet("attention-types")]
    public async Task<IActionResult> AttentionTypes(CancellationToken ct) =>
        Ok(await _db.AttentionTypes.OrderBy(t => t.Description)
            .Select(t => new { t.AttentionTypeId, t.Description, t.Prefix }).ToListAsync(ct));

    [HttpGet("attention-statuses")]
    public async Task<IActionResult> AttentionStatuses(CancellationToken ct) =>
        Ok(await _db.AttentionStatuses.OrderBy(s => s.StatusId).Select(s => new { s.StatusId, s.Description }).ToListAsync(ct));

    [HttpGet("method-payments")]
    public async Task<IActionResult> MethodPayments(CancellationToken ct) =>
        Ok(await _db.MethodPayments.OrderBy(m => m.MethodPaymentId)
            .Select(m => new { m.MethodPaymentId, m.Description }).ToListAsync(ct));

    [HttpGet("contract-statuses")]
    public async Task<IActionResult> ContractStatuses(CancellationToken ct) =>
        Ok(await _db.StatusContracts.Select(s => new { s.StatusId, s.Description }).ToListAsync(ct));
}
