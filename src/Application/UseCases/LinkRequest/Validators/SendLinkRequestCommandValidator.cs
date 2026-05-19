using Application.UseCases.LinkRequest.Commands;
using FluentValidation;

namespace Application.UseCases.LinkRequest.Validators;

internal sealed class SendLinkRequestCommandValidator : AbstractValidator<SendLinkRequestCommand>
{
    public SendLinkRequestCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("El ID del paciente es requerido");

        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithMessage("El ID del doctor es requerido");
    }
}
