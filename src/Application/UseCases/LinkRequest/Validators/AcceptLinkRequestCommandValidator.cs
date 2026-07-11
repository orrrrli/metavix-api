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

        // MedicalRecordNumber is optional. When the doctor provides one,
        // we enforce the format. When omitted, the handler will assign
        // the next available MRN for the current year.
        When(x => !string.IsNullOrEmpty(x.MedicalRecordNumber), () =>
        {
            RuleFor(x => x.MedicalRecordNumber!)
                .Matches(@"^MRN-\d{4}-\d{6}$")
                .WithMessage("Formato inválido. Use MRN-AAAA-NNNNNN (ej. MRN-2026-000001)");
        });
    }
}
