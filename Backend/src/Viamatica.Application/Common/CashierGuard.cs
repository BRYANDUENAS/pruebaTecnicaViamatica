using Microsoft.EntityFrameworkCore;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;

namespace Viamatica.Application.Common;

public static class CashierGuard
{
    /// <summary>Los procesos de caja exigen que el cajero tenga su caja abierta.</summary>
    public static async Task EnsureCashOpenAsync(this IAppDbContext db, ICurrentUser current, CancellationToken ct)
    {
        var open = await db.Cashes.AnyAsync(c => c.CurrentUserId == current.UserId, ct);
        if (!open) throw DomainException.Conflict("Debe abrir la caja antes de realizar procesos de caja.");
    }
}
