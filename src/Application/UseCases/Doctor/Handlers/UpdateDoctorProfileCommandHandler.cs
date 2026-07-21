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
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — caller is updating their own doctor profile, so a single
        //    by-userId lookup is the right granularity (no doctorId is supplied
        //    in the command). A null result means the authenticated user simply
        //    has no doctor profile yet — that is a missing resource, not a
        //    permissions failure, so surface DoctorNotFound (not Forbidden).
        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
        if (doctor is null)
            return DoctorErrors.DoctorNotFound;

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
