namespace Application.UseCases.Auth.Common;

public sealed record MeResult(
    Guid    UserId,
    Guid?   PatientId,
    Guid?   DoctorId,
    string  Email,
    string  Role,
    string  FullName);
