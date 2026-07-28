using Application.UseCases.ClinicalGoals.Common;
using Application.Common.Messaging;

namespace Application.UseCases.ClinicalGoals.Queries;

public sealed record GetClinicalGoalsQuery(
    Guid DoctorId,
    Guid PatientId) : IQuery<ErrorOr<ClinicalGoalsResult>>;
