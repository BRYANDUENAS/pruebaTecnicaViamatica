using Microsoft.EntityFrameworkCore;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Abstractions;

/// <summary>Contrato de persistencia. Lo implementa Infrastructure con EF Core.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Rol> Roles { get; }
    DbSet<UserStatus> UserStatuses { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<RolMenu> RolMenus { get; }
    DbSet<Cash> Cashes { get; }
    DbSet<UserCash> UserCashes { get; }
    DbSet<Turn> Turns { get; }
    DbSet<Attention> Attentions { get; }
    DbSet<AttentionType> AttentionTypes { get; }
    DbSet<AttentionStatus> AttentionStatuses { get; }
    DbSet<Client> Clients { get; }
    DbSet<Service> Services { get; }
    DbSet<Device> Devices { get; }
    DbSet<MethodPayment> MethodPayments { get; }
    DbSet<StatusContract> StatusContracts { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<Payment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Crea el turno y su atención pendiente con la función de base de datos
    /// <c>fn_create_turn</c>, que genera la descripción (ej. AC0001). Devuelve el id.
    /// </summary>
    Task<int> CreateTurnAsync(string attentionTypeId, int cashId, int gestorId, CancellationToken ct = default);
}

/// <summary>Cifrado reversible para datos sensibles (correo, identificación, claims).</summary>
public interface IFieldCipher
{
    string Encrypt(string plain);
    string Decrypt(string cipher);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(User user);
}

/// <summary>Usuario autenticado de la petición en curso.</summary>
public interface ICurrentUser
{
    int UserId { get; }
    string Username { get; }
    int RolId { get; }
    bool IsAdmin { get; }
    bool IsGestor { get; }
    bool IsCajero { get; }
}
