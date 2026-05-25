using Application.UseCases.DailyRecord.Commands;
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
                    .InclusiveBetween(20, 600)
                    .WithMessage("El valor de glucosa debe estar entre 20 y 600 mg/dL");
            });
    }
}
