using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Commands;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Mappers;

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
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
        if (doctor is null)
            return DoctorErrors.DoctorNotFound;

        await _doctorRepository.UpdateProfileAsync(
            doctor.Id,
            command.LicenseNumber,
            command.Speciality,
            cancellationToken);

        return DoctorProfileMapper.ToResult(doctor, command.LicenseNumber, command.Speciality);
    }
}
