namespace Contracts.LinkRequest.Request;

public record SendLinkRequestRequest(
    Guid PatientId,
    Guid DoctorId);
