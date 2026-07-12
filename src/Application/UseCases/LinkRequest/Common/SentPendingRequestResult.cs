namespace Application.UseCases.LinkRequest.Common;

public sealed record SentPendingRequestResult(
    Guid RequestId,
    Guid DoctorId,
    string DoctorFirstName,
    string DoctorPaternalLastName,
    string Speciality,
    DateTime CreatedAt);
