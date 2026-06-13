namespace Domain.Models;

public class Doctor
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string PaternalLastName { get; set; } = string.Empty;
    public string MaternalLastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid UserId { get; set; }
    public string? Curp { get; set; }
    public string? IneNumber { get; set; }
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Patient> Patients { get; set; } = [];
    public ICollection<Admission> Admissions { get; set; } = [];
    public ICollection<PatientDoctorRequest> LinkRequests { get; set; } = [];
}
