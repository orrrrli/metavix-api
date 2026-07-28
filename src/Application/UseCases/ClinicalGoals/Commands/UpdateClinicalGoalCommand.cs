using Application.UseCases.ClinicalGoals.Common;
using Application.Common.Messaging;

namespace Application.UseCases.ClinicalGoals.Commands;

public sealed record UpdateClinicalGoalCommand(
    Guid DoctorId,
    Guid PatientId,
    Guid GoalId,
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh) : ITransactionalCommand<ErrorOr<ClinicalGoalResult>>;
