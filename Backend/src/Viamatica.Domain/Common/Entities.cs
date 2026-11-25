namespace Viamatica.Domain.Common;

/// <summary>Marca las entidades con eliminación lógica: nunca se borran físicamente.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

/// <summary>Error de regla de negocio. La API lo traduce a una respuesta 4xx.</summary>
public class DomainException : Exception
{
    public int StatusCode { get; }

    public DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public static DomainException NotFound(string what) => new($"{what} no existe.", 404);
    public static DomainException Conflict(string message) => new(message, 409);
    public static DomainException Forbidden(string message) => new(message, 403);
}
