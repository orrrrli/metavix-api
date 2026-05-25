namespace Application.UseCases.Auth.Common;

public sealed record LoginResult(
    Guid UserId,
    Guid? PatientId,
    Guid? DoctorId,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
