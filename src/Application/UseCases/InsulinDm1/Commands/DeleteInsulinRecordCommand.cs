namespace Application.UseCases.InsulinDm1.Commands;

public sealed record DeleteInsulinRecordCommand(
    Guid PatientId,
    Guid RecordId) : IRequest<ErrorOr<Deleted>>;
