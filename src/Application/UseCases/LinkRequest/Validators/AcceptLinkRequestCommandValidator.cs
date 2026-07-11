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

        RuleFor(x => x.MedicalRecordNumber)
            .NotEmpty().WithMessage("El número de historia clínica es requerido")
            .Matches(@"^MRN-\d{4}-\d{6}$")
            .WithMessage("Formato inválido. Use MRN-AAAA-NNNNNN (ej. MRN-2026-000001)");
    }
}
