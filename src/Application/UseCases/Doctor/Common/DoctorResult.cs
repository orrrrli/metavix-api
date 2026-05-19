namespace Application.UseCases.Doctor.Common;

public sealed record DoctorResult(
    Guid Id,
    string FirstName,
    string LastName,
    string Speciality,
    string Email);
