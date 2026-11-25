namespace Viamatica.Domain.Common;

public static class RoleCodes
{
    public const int AdminId = 1;
    public const int GestorId = 2;
    public const int CajeroId = 3;

    public const string Admin = "Administrador";
    public const string Gestor = "Gestor";
    public const string Cajero = "Cajero";
}

public static class UserStatusCodes
{
    public const string Active = "ACT";
    public const string Inactive = "INA";
    public const string Blocked = "BLO";
    public const string Pending = "PEN";
}

public static class ContractStatusCodes
{
    public const string Vigente = "VIG";
    public const string Sustituido = "SUS";
    public const string Renovado = "REN";
    public const string Cancelado = "CAN";

    /// <summary>Estados en los que el contrato sigue activo para el cliente.</summary>
    public static readonly string[] Open = [Vigente, Renovado];
}

public static class AttentionStatusCodes
{
    public const int Pending = 1;
    public const int InProgress = 2;
    public const int Attended = 3;
    public const int Cancelled = 4;
}

public static class AttentionTypeCodes
{
    public const string AtencionCliente = "ATC";
    public const string PagoServicio = "PGS";
    public const string Contratacion = "CTR";
    public const string CambioServicio = "CMS";
    public const string CambioFormaPago = "CFP";
    public const string Cancelacion = "CAN";
}
