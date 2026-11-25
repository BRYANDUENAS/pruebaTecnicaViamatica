using Viamatica.Domain.Common;

namespace Viamatica.Domain.Entities;

public class User : ISoftDelete
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>Cifrado con AES en la base de datos.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Cifrado con AES en la base de datos.</summary>
    public string Identification { get; set; } = null!;

    /// <summary>Hash BCrypt; no es reversible.</summary>
    public string Password { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    public string StatusId { get; set; } = UserStatusCodes.Active;
    public UserStatus Status { get; set; } = null!;

    public DateTime CreationDate { get; set; }
    public int? UserCreate { get; set; }
    public int? UserApproval { get; set; }
    public DateTime? DateApproval { get; set; }

    public bool SessionActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public int FailedAttempts { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserCash> UserCashes { get; set; } = new List<UserCash>();
}

public class Rol
{
    public int RolId { get; set; }
    public string RolName { get; set; } = null!;
}

public class UserStatus
{
    public string StatusId { get; set; } = null!;
    public string Description { get; set; } = null!;
}

/// <summary>Opción de menú. Se entrega al frontend según el rol.</summary>
public class MenuItem
{
    public int MenuItemId { get; set; }
    public string Label { get; set; } = null!;
    public string Route { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int Order { get; set; }
    public ICollection<RolMenu> Roles { get; set; } = new List<RolMenu>();
}

public class RolMenu
{
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;
}
