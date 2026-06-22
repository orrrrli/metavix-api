using Application.UseCases.Goals.Common;
using Domain.Enums;

namespace Application.UseCases.Goals.Commands;

public sealed record EvaluateGoalsCommand(
    Guid PatientId,
    EvaluationTrigger TriggeredBy) : IRequest<ErrorOr<EvaluateGoalsResult>>;
