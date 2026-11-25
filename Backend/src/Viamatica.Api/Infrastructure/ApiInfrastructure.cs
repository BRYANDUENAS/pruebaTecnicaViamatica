using System.Security.Claims;
using FluentValidation;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;

namespace Viamatica.Api.Infrastructure;

/// <summary>Lee el usuario autenticado de las claims ya descifradas.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User;
    }

    public int UserId => int.TryParse(_principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string Username => _principal?.FindFirstValue(ClaimTypes.Name) ?? "anónimo";
    public int RolId => int.TryParse(_principal?.FindFirstValue("rid"), out var id) ? id : 0;
    public bool IsAdmin => RolId == RoleCodes.AdminId;
    public bool IsGestor => RolId == RoleCodes.GestorId;
    public bool IsCajero => RolId == RoleCodes.CajeroId;
}

public record ErrorResponse(string Message, IDictionary<string, string[]>? Errors = null);

/// <summary>Traduce las excepciones a respuestas JSON uniformes y las registra con Serilog.</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            _logger.LogWarning("Validación fallida en {Path}: {@Errors}", context.Request.Path, errors);
            await Write(context, 400, new ErrorResponse("Los datos enviados no son válidos.", errors));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Regla de negocio en {Path}: {Message}", context.Request.Path, ex.Message);
            await Write(context, ex.StatusCode, new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, 500, new ErrorResponse("Ocurrió un error inesperado. Intente nuevamente."));
        }
    }

    private static Task Write(HttpContext context, int status, ErrorResponse body)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(body);
    }
}
