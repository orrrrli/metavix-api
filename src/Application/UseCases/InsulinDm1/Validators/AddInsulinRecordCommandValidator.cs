using Application.Common.Constants;
using Application.UseCases.InsulinDm1.Commands;
using FluentValidation;

namespace Application.UseCases.InsulinDm1.Validators;

internal sealed class AddInsulinRecordCommandValidator : AbstractValidator<AddInsulinRecordCommand>
{
    public AddInsulinRecordCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("El ID del paciente es requerido.");

        RuleFor(x => x.RecordDate)
            .NotEmpty()
            .WithMessage("La fecha del registro es requerida.");

        RuleFor(x => x.GlucoseBefore)
            .InclusiveBetween(AdaGoalConstants.MinGlucoseReadingMgDl, AdaGoalConstants.MaxGlucoseReadingMgDl)
            .When(x => x.GlucoseBefore.HasValue)
            .WithMessage($"La glucosa preprandial debe estar entre {AdaGoalConstants.MinGlucoseReadingMgDl} y {AdaGoalConstants.MaxGlucoseReadingMgDl} mg/dL.");

        RuleFor(x => x.GlucoseAfter)
            .InclusiveBetween(AdaGoalConstants.MinGlucoseReadingMgDl, AdaGoalConstants.MaxGlucoseReadingMgDl)
            .When(x => x.GlucoseAfter.HasValue)
            .WithMessage($"La glucosa postprandial debe estar entre {AdaGoalConstants.MinGlucoseReadingMgDl} y {AdaGoalConstants.MaxGlucoseReadingMgDl} mg/dL.");

        RuleFor(x => x.TotalCarbs)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalCarbs.HasValue)
            .WithMessage("Los gramos de HC no pueden ser negativos.");

        RuleFor(x => x.DoseApplied)
            .GreaterThan(0)
            .When(x => x.DoseApplied.HasValue)
            .WithMessage("La dosis aplicada debe ser mayor a 0.");

        RuleFor(x => x.HowIFelt)
            .MaximumLength(200)
            .When(x => x.HowIFelt is not null);
    }
}
