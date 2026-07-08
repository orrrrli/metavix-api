using Application.UseCases.ClinicalGoals.Common;

namespace Application.UseCases.ClinicalGoals.Queries;

public sealed record GetClinicalGoalsQuery(
    Guid DoctorId,
    Guid PatientId) : IRequest<ErrorOr<List<ClinicalGoalResult>>>;
