namespace Application.UseCases.Doctor.Common;

public sealed record DoctorResult(
    Guid Id,
    string FirstName,
    string PaternalLastName,
    string Speciality,
    string Email);
