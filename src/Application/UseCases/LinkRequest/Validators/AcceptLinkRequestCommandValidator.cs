using Application.UseCases.LinkRequest.Commands;
using FluentValidation;

namespace Application.UseCases.LinkRequest.Validators;

internal sealed class AcceptLinkRequestCommandValidator : AbstractValidator<AcceptLinkRequestCommand>
{
    public AcceptLinkRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("El ID de la solicitud es requerido");
    }
}
