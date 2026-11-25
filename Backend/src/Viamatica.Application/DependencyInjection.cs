using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Viamatica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<Auth.AuthService>();
        services.AddScoped<Users.UserService>();
        services.AddScoped<Clients.ClientService>();
        services.AddScoped<Cashes.CashService>();
        services.AddScoped<Turns.TurnService>();
        return services;
    }
}
