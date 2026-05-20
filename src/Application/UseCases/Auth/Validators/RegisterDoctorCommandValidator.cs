using Application.UseCases.Auth.Commands;
using FluentValidation;

namespace Application.UseCases.Auth.Validators;

internal sealed class RegisterDoctorCommandValidator : AbstractValidator<RegisterDoctorCommand>
{
    public RegisterDoctorCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("El nombre es requerido");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("El apellido es requerido");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(12)
            .WithMessage("La contraseña debe tener al menos 12 caracteres")
            .Matches("[A-Z]")
            .WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches("[0-9]")
            .WithMessage("La contraseña debe contener al menos un número")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("La contraseña debe contener al menos un carácter especial");
    }
}
