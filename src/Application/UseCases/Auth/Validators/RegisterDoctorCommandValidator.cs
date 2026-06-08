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

        RuleFor(x => x.PaternalLastName)
            .NotEmpty()
            .WithMessage("El apellido paterno es requerido");

        RuleFor(x => x.MaternalLastName)
            .NotEmpty()
            .WithMessage("El apellido materno es requerido");

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

        RuleFor(x => x.LicenseNumber)
            .NotEmpty()
            .WithMessage("El número de cédula profesional es requerido")
            .Matches(@"^\d{5,12}$")
            .WithMessage("El número de cédula debe contener entre 5 y 12 dígitos");

        RuleFor(x => x.Speciality)
            .NotEmpty()
            .WithMessage("La especialidad es requerida");
    }
}
