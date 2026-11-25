using FluentValidation;
using Viamatica.Application.Users;
using Viamatica.Domain.Entities;

namespace Viamatica.Application.Clients;

public record ClientDto(
    int ClientId,
    string Name,
    string LastName,
    string Identification,
    string Email,
    string PhoneNumber,
    string Address,
    string ReferenceAddress,
    bool Active,
    DateTime CreationDate)
{
    public static ClientDto From(Client c) => new(
        c.ClientId, c.Name, c.LastName, c.Identification, c.Email, c.PhoneNumber,
        c.Address, c.ReferenceAddress, c.Active, c.CreationDate);
}

public record CreateClientRequest(
    string Name,
    string LastName,
    string Identification,
    string Email,
    string PhoneNumber,
    string Address,
    string ReferenceAddress);

/// <summary>La identificación no se actualiza: es la clave con la que se valida el registro único.</summary>
public record UpdateClientRequest(
    string Name,
    string LastName,
    string Email,
    string PhoneNumber,
    string Address,
    string ReferenceAddress);

public record ChangeClientStatusRequest(bool Active);

public static class ClientRules
{
    public static IRuleBuilderOptions<T, string> ValidPhone<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("El teléfono es obligatorio.")
        .Matches(@"^09\d{8,11}$")
        .WithMessage("El teléfono debe empezar con 09, tener solo números y entre 10 y 13 dígitos.");

    public static IRuleBuilderOptions<T, string> ValidAddress<T>(this IRuleBuilder<T, string> rule, string field) => rule
        .NotEmpty().WithMessage($"{field} es obligatoria.")
        .Length(20, 100).WithMessage($"{field} debe tener entre 20 y 100 caracteres.");

    public static IRuleBuilderOptions<T, string> ValidPersonName<T>(this IRuleBuilder<T, string> rule, string field) => rule
        .NotEmpty().WithMessage($"{field} es obligatorio.")
        .MaximumLength(50).WithMessage($"{field} admite máximo 50 caracteres.");

    public static IRuleBuilderOptions<T, string> ValidClientEmail<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("El correo es obligatorio.")
        .EmailAddress().WithMessage("El correo no es válido.")
        .MaximumLength(120).WithMessage("El correo admite máximo 120 caracteres.");
}

public class CreateClientValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name).ValidPersonName("El nombre");
        RuleFor(x => x.LastName).ValidPersonName("El apellido");
        RuleFor(x => x.Identification).ValidIdentification();
        RuleFor(x => x.Email).ValidClientEmail();
        RuleFor(x => x.PhoneNumber).ValidPhone();
        RuleFor(x => x.Address).ValidAddress("La dirección");
        RuleFor(x => x.ReferenceAddress).ValidAddress("La referencia de la dirección");
    }
}

public class UpdateClientValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Name).ValidPersonName("El nombre");
        RuleFor(x => x.LastName).ValidPersonName("El apellido");
        RuleFor(x => x.Email).ValidClientEmail();
        RuleFor(x => x.PhoneNumber).ValidPhone();
        RuleFor(x => x.Address).ValidAddress("La dirección");
        RuleFor(x => x.ReferenceAddress).ValidAddress("La referencia de la dirección");
    }
}
