using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;
using Viamatica.Infrastructure.Persistence;
using Viamatica.Infrastructure.Security;

namespace Viamatica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SecurityOptions>(config.GetSection(SecurityOptions.Section));
        services.AddSingleton<IFieldCipher, AesFieldCipher>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(config.GetConnectionString("Default"))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    /// <summary>Aplica las migraciones pendientes y crea el administrador inicial si no existe.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await db.Database.MigrateAsync();

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.RolId == RoleCodes.AdminId)) return;

        var seed = config.GetSection("Seed:Admin");
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new User
        {
            Username = seed["Username"] ?? "admin001",
            FirstName = "Administrador",
            LastName = "General",
            Email = seed["Email"] ?? "admin@viamatica.local",
            Identification = seed["Identification"] ?? "0900000001",
            Password = hasher.Hash(seed["Password"] ?? "Admin1234"),
            RolId = RoleCodes.AdminId,
            StatusId = UserStatusCodes.Active,
            CreationDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Usuario administrador inicial creado");
    }
}
