namespace Contracts.Auth;

public record RegisterPatientRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
