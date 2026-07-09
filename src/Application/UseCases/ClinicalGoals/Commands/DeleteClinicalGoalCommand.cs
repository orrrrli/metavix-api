namespace Application.UseCases.ClinicalGoals.Commands;

public sealed record DeleteClinicalGoalCommand(
    Guid DoctorId,
    Guid PatientId,
    Guid GoalId) : IRequest<ErrorOr<Deleted>>;
