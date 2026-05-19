namespace Application.UseCases.LinkRequest.Common;

public sealed record LinkRequestResult(
    Guid RequestId,
    Guid PatientId,
    Guid DoctorId,
    string Status,
    DateTime CreatedAt);
