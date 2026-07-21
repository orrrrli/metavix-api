using Application.Common.Authorization;
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
        var userIdResult = CurrentUserAccess.RequireUserId(_currentUser);
        if (userIdResult.IsError)
            return userIdResult.FirstError;
        var userId = userIdResult.Value;

        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
        if (doctor is null)
            return DoctorErrors.DoctorNotFound;

        // UpdateProfileAsync issues a targeted ExecuteUpdate (only LicenseNumber,
        // Speciality and UpdatedAt), so the AsNoTracking `doctor` we loaded still
        // holds the OLD LicenseNumber/Speciality. Return the command's new values
        // for those two fields rather than the stale loaded ones.
        await _doctorRepository.UpdateProfileAsync(
            doctor.Id,
            command.LicenseNumber,
            command.Speciality,
            cancellationToken);

        return new DoctorProfileResult(
            doctor.Id,
            doctor.FirstName,
            doctor.MiddleName,
            doctor.PaternalLastName,
            doctor.MaternalLastName,
            command.LicenseNumber,
            command.Speciality,
            doctor.Email,
            doctor.Phone,
            doctor.Curp,
            doctor.IneNumber,
            doctor.IsVerified,
            doctor.IsActive,
            doctor.CreatedAt);
    }
}
