using Application.UseCases.LinkRequest.Commands;
using FluentValidation;

namespace Application.UseCases.LinkRequest.Validators;

internal sealed class RejectLinkRequestCommandValidator : AbstractValidator<RejectLinkRequestCommand>
{
    public RejectLinkRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithMessage("El ID de la solicitud es requerido");
    }
}
