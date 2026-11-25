namespace Viamatica.Application.Common;

/// <summary>
/// Las fechas se guardan en UTC, pero "el día" del negocio es el de Ecuador
/// (America/Guayaquil, UTC-5, sin horario de verano).
/// </summary>
public static class BusinessDay
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow + Offset);

    /// <summary>Rango UTC [inicio, fin) que cubre los días locales indicados, ambos inclusive.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) RangeUtc(DateOnly from, DateOnly to)
    {
        var start = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue) - Offset, DateTimeKind.Utc);
        return (start, end);
    }

    public static (DateTime FromUtc, DateTime ToUtc) RangeUtc(DateOnly day) => RangeUtc(day, day);
}
