using Application.UseCases.InsulinDm1.Commands;
using FluentValidation;

namespace Application.UseCases.InsulinDm1.Validators;

internal sealed class UpsertInsulinProfileCommandValidator : AbstractValidator<UpsertInsulinProfileCommand>
{
    public UpsertInsulinProfileCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("El ID del paciente es requerido.");

        RuleFor(x => x.Ric)
            .GreaterThan(0)
            .When(x => x.Ric.HasValue)
            .WithMessage("La relación insulina:carbono debe ser mayor a 0.");

        RuleFor(x => x.SensitivityFactor)
            .GreaterThan(0)
            .When(x => x.SensitivityFactor.HasValue)
            .WithMessage("El factor de sensibilidad debe ser mayor a 0.");

        RuleFor(x => x.TargetGlucose)
            .InclusiveBetween(60, 250)
            .When(x => x.TargetGlucose.HasValue)
            .WithMessage("La glucosa objetivo debe estar entre 60 y 250 mg/dL.");

        RuleFor(x => x.InsulinName)
            .MaximumLength(80)
            .When(x => x.InsulinName is not null);

        RuleFor(x => x.DoctorName)
            .MaximumLength(120)
            .When(x => x.DoctorName is not null);

        RuleFor(x => x.DoctorPhone)
            .MaximumLength(30)
            .When(x => x.DoctorPhone is not null);
    }
}
