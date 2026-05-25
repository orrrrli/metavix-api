namespace Contracts.Auth;

public record AuthResponse(
    Guid UserId,
    Guid? PatientId,
    Guid? DoctorId,
    DateTime ExpiresAt,
    string Email,
    string Role,
    string FullName);
