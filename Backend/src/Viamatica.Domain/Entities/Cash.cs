using Viamatica.Domain.Common;

namespace Viamatica.Domain.Entities;

public class Cash : ISoftDelete
{
    public const int MaxUsers = 2;

    public int CashId { get; set; }
    public string CashDescription { get; set; } = null!;
    public bool Active { get; set; } = true;

    /// <summary>
    /// Cajero que está operando la caja en este momento. Solo uno a la vez:
    /// los dos usuarios asignados no pueden trabajar en la caja simultáneamente.
    /// </summary>
    public int? CurrentUserId { get; set; }
    public User? CurrentUser { get; set; }
    public DateTime? OpenedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserCash> UserCashes { get; set; } = new List<UserCash>();
}

public class UserCash
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CashId { get; set; }
    public Cash Cash { get; set; } = null!;
    public int AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class Turn : ISoftDelete
{
    public int TurnId { get; set; }

    /// <summary>2 letras mayúsculas y 4 números, según el tipo de atención (ej. AC0001).</summary>
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }

    public int CashId { get; set; }
    public Cash Cash { get; set; } = null!;

    /// <summary>Gestor que asignó el turno.</summary>
    public int UserGestorId { get; set; }
    public User UserGestor { get; set; } = null!;

    public string AttentionTypeId { get; set; } = null!;
    public AttentionType AttentionType { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Attention> Attentions { get; set; } = new List<Attention>();
}

public class Attention
{
    public int AttentionId { get; set; }
    public int TurnId { get; set; }
    public Turn Turn { get; set; } = null!;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public string AttentionTypeId { get; set; } = null!;
    public AttentionType AttentionType { get; set; } = null!;
    public int StatusId { get; set; }
    public AttentionStatus Status { get; set; } = null!;

    /// <summary>Cajero que atendió el turno.</summary>
    public int? UserCashierId { get; set; }
    public User? UserCashier { get; set; }
    public DateTime? AttendedAt { get; set; }
}

public class AttentionType
{
    public string AttentionTypeId { get; set; } = null!;
    public string Description { get; set; } = null!;

    /// <summary>Prefijo de 2 letras para la descripción del turno.</summary>
    public string Prefix { get; set; } = null!;
}

public class AttentionStatus
{
    public int StatusId { get; set; }
    public string Description { get; set; } = null!;
}
