using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Commands;
using Application.UseCases.Doctor.Common;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class UpdateDoctorProfileCommandHandler
    : IRequestHandler<UpdateDoctorProfileCommand, ErrorOr<DoctorProfileResult>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdateDoctorProfileCommandHandler(
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser)
    {
        _doctorRepository = doctorRepository;
        _currentUser      = currentUser;
    }

    public async Task<ErrorOr<DoctorProfileResult>> Handle(
        UpdateDoctorProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var doctorId = await _doctorRepository.GetDoctorIdByUserIdAsync(_currentUser.UserId.Value);
        if (doctorId is null)
            return AuthErrors.Forbidden;

        await _doctorRepository.UpdateProfileAsync(
            doctorId.Value,
            command.LicenseNumber,
            command.Speciality,
            cancellationToken);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId.Value);
        if (doctor is null)
            return DoctorErrors.DoctorNotFound;

        return new DoctorProfileResult(
            doctor.Id,
            doctor.FirstName,
            doctor.MiddleName,
            doctor.PaternalLastName,
            doctor.MaternalLastName,
            doctor.LicenseNumber,
            doctor.Speciality,
            doctor.Email,
            doctor.Phone,
            doctor.Curp,
            doctor.IneNumber,
            doctor.IsVerified,
            doctor.IsActive,
            doctor.CreatedAt);
    }
}
