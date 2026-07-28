using Application.UseCases.ClinicalGoals.Common;
using Application.Common.Messaging;

namespace Application.UseCases.ClinicalGoals.Commands;

public sealed record CreateClinicalGoalCommand(
    Guid DoctorId,
    Guid PatientId,
    string ParameterId,
    decimal? CustomOutOfRangeLow,
    decimal? CustomAtRiskLow,
    decimal? CustomAtRiskHigh,
    decimal? CustomOutOfRangeHigh) : ITransactionalCommand<ErrorOr<ClinicalGoalResult>>;
