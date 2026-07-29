using Application.UseCases.Goals.Common;
using Domain.Enums;
using Application.Common.Messaging;

namespace Application.UseCases.Goals.Commands;

public sealed record EvaluateGoalsCommand(
    Guid PatientId,
    EvaluationTrigger TriggeredBy) : ICommand<ErrorOr<EvaluateGoalsResult>>;
