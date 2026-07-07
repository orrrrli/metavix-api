using Application.UseCases.Patient.Commands;
using FluentValidation;

namespace Application.UseCases.Patient.Validators;

internal sealed class UpdatePatientProfileCommandValidator : AbstractValidator<UpdatePatientProfileCommand>
{
    public UpdatePatientProfileCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty();
    }
}
