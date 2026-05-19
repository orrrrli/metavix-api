using Application.UseCases.LabResult.Commands;
using FluentValidation;

namespace Application.UseCases.LabResult.Validators;

internal sealed class AddLabResultCommandValidator : AbstractValidator<AddLabResultCommand>
{
    public AddLabResultCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("El ID del paciente es requerido");

        RuleFor(x => x.SampleDate)
            .NotEmpty()
            .WithMessage("La fecha de la muestra es requerida");
    }
}
