using FluentValidation;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Users;

public record UserDto(
    int UserId,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string Identification,
    int RolId,
    string RolName,
    string StatusId,
    string StatusDescription,
    DateTime CreationDate,
    int? UserCreate,
    int? UserApproval,
    DateTime? DateApproval,
    bool SessionActive,
    DateTime? LastLogin)
{
    /// <summary>Requiere que el usuario se haya cargado con Rol y Status.</summary>
    public static UserDto From(User u) => new(
        u.UserId, u.Username, u.FirstName, u.LastName, u.Email, u.Identification,
        u.RolId, u.Rol.RolName, u.StatusId, u.Status.Description, u.CreationDate,
        u.UserCreate, u.UserApproval, u.DateApproval, u.SessionActive, u.LastLogin);
}

public record CreateUserRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string Identification,
    int RolId);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Identification,
    int RolId);

public record ChangeUserStatusRequest(string StatusId);

public record BulkRowResult(int Row, string Key, bool Success, string? Error);

public record BulkResult(int Total, int Created, int Failed, IReadOnlyList<BulkRowResult> Rows);

public static class UserRules
{
    public const string UsernamePattern = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,20}$";
    public const string PasswordPattern = @"^(?=.*\d)(?=.*[A-Z]).{8,30}$";

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("La contraseña es obligatoria.")
        .Matches(PasswordPattern)
        .WithMessage("La contraseña debe tener entre 8 y 30 caracteres, al menos un número y una letra mayúscula.");

    public static IRuleBuilderOptions<T, string> ValidIdentification<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("La identificación es obligatoria.")
        .Matches(@"^\d{10,13}$").WithMessage("La identificación debe tener entre 10 y 13 dígitos, solo números.");

    public static IRuleBuilderOptions<T, int> AssignableRole<T>(this IRuleBuilder<T, int> rule) => rule
        .Must(r => r is RoleCodes.GestorId or RoleCodes.CajeroId)
        .WithMessage("Solo se pueden gestionar usuarios gestores o cajeros.");
}

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .Matches(UserRules.UsernamePattern)
            .WithMessage("El nombre de usuario debe tener entre 8 y 20 caracteres, solo letras y números, con al menos una letra y un número.");
        RuleFor(x => x.Password).ValidPassword();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100).WithMessage("El correo no es válido.");
        RuleFor(x => x.Identification).ValidIdentification();
        RuleFor(x => x.RolId).AssignableRole();
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100).WithMessage("El correo no es válido.");
        RuleFor(x => x.Identification).ValidIdentification();
        RuleFor(x => x.RolId).AssignableRole();
    }
}

public class ChangeUserStatusValidator : AbstractValidator<ChangeUserStatusRequest>
{
    public ChangeUserStatusValidator()
    {
        RuleFor(x => x.StatusId)
            .Must(s => s is UserStatusCodes.Active or UserStatusCodes.Inactive or UserStatusCodes.Blocked)
            .WithMessage("El estado debe ser ACT, INA o BLO.");
    }
}
