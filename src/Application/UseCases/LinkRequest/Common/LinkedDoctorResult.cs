namespace Application.UseCases.LinkRequest.Common;

public sealed record LinkedDoctorResult(
    Guid RequestId,
    Guid DoctorId,
    string DoctorFirstName,
    string DoctorLastName,
    string Speciality,
    string Email,
    DateTime LinkedAt);
