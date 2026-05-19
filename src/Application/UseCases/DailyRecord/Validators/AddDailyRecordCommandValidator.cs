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
    }
}
