namespace Contracts.Auth;

public sealed record MeResponse(
    Guid    UserId,
    Guid?   PatientId,
    Guid?   DoctorId,
    string  Email,
    string  Role,
    string  FullName);
