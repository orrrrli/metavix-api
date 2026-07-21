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
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — caller is fetching their own doctor profile, so a single
        //    by-userId lookup is the right granularity (no doctorId is supplied
        //    in this query). Null collapses "user has no doctor yet" and any
        //    other miss into one error path.
        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
        if (doctor is null)
            return AuthErrors.Forbidden;

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
