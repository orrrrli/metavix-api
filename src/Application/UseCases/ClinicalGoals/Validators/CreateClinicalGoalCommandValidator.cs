using Application.UseCases.ClinicalGoals.Commands;
using FluentValidation;

namespace Application.UseCases.ClinicalGoals.Validators;

internal sealed class CreateClinicalGoalCommandValidator : AbstractValidator<CreateClinicalGoalCommand>
{
    public CreateClinicalGoalCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ParameterId).NotEmpty();

        RuleFor(x => x).Custom((cmd, context) =>
            ClinicalGoalThresholdRules.Validate(
                context, cmd.CustomOutOfRangeLow, cmd.CustomAtRiskLow, cmd.CustomAtRiskHigh, cmd.CustomOutOfRangeHigh));
    }
}
