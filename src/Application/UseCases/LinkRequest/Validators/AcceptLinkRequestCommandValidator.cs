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
        // we enforce the format. When omitted, the handler auto-assigns a
        // timestamp-derived MRN.
        When(x => !string.IsNullOrEmpty(x.MedicalRecordNumber), () =>
        {
            RuleFor(x => x.MedicalRecordNumber!)
                .Matches(@"^MRN-\d{8}-\d{9}$")
                .WithMessage("Formato inválido. Use MRN-AAAAMMDD-HHMMSSmmm (ej. MRN-20260711-153045123)");
        });
    }
}
