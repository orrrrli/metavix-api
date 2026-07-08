using Application.UseCases.ClinicalGoals.Common;

namespace Application.UseCases.ClinicalGoals.Commands;

public sealed record UpdateClinicalGoalCommand(
    Guid DoctorId,
    Guid PatientId,
    Guid GoalId,
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh) : IRequest<ErrorOr<ClinicalGoalResult>>;
