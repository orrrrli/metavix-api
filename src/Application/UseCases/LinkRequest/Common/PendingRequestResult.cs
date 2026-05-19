namespace Application.UseCases.LinkRequest.Common;

public sealed record PendingRequestResult(
    Guid RequestId,
    Guid PatientId,
    string PatientFirstName,
    string PatientLastName,
    string? PatientEmail,
    DateTime CreatedAt);
