using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetMyDoctorProfileQueryHandler
    : IRequestHandler<GetMyDoctorProfileQuery, ErrorOr<DoctorProfileResult>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyDoctorProfileQueryHandler(
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser)
    {
        _doctorRepository = doctorRepository;
        _currentUser      = currentUser;
    }

    public async Task<ErrorOr<DoctorProfileResult>> Handle(
        GetMyDoctorProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize
        var userIdResult = CurrentUserAccess.RequireUserId(_currentUser);
        if (userIdResult.IsError)
            return userIdResult.FirstError;
        var userId = userIdResult.Value;

        // 2. Load — caller is fetching their own doctor profile, so a single
        //    by-userId lookup is the right granularity (no doctorId is supplied
        //    in this query). A null result means the authenticated user simply
        //    has no doctor profile yet — that is a missing resource, not a
        //    permissions failure, so surface DoctorNotFound (not Forbidden).
        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
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
