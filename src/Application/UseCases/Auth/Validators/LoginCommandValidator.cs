using Application.UseCases.Auth.Commands;
using FluentValidation;

namespace Application.UseCases.Auth.Validators;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(12)
            .WithMessage("La contraseña debe tener al menos 12 caracteres");
    }
}
