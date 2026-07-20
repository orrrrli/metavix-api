using Application.Common.Constants;
using Application.UseCases.DailyRecord.Commands;
using Domain.Enums;
using FluentValidation;

namespace Application.UseCases.DailyRecord.Validators;

internal sealed class AddDailyRecordCommandValidator : AbstractValidator<AddDailyRecordCommand>
{
    public AddDailyRecordCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("El ID del paciente es requerido");

        RuleFor(x => x.RecordDate)
            .NotEmpty()
            .WithMessage("La fecha del registro es requerida");

        RuleForEach(x => x.GlucoseReadings)
            .ChildRules(g =>
            {
                g.RuleFor(r => r.ValueMgDl)
                    .InclusiveBetween(AdaGoalConstants.MinGlucoseReadingMgDl, AdaGoalConstants.MaxGlucoseReadingMgDl)
                    .WithMessage($"El valor de glucosa debe estar entre {AdaGoalConstants.MinGlucoseReadingMgDl} y {AdaGoalConstants.MaxGlucoseReadingMgDl} mg/dL");

                g.RuleFor(r => r.PostprandialWindow)
                    .Must((reading, _) => reading.PostprandialWindow is null
                        || reading.ReadingType is GlucoseReadingType.PostBreakfast
                            or GlucoseReadingType.PostLunch or GlucoseReadingType.PostDinner)
                    .WithMessage("El marcador 1h/2h solo aplica a lecturas post-comida");
            });
    }
}
