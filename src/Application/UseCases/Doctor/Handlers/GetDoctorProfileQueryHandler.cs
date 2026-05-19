using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetDoctorProfileQueryHandler
    : IRequestHandler<GetDoctorProfileQuery, ErrorOr<DoctorProfileResult>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;

    public GetDoctorProfileQueryHandler(
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser)
    {
        _doctorRepository = doctorRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<DoctorProfileResult>> Handle(
        GetDoctorProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerDoctorId = await _doctorRepository.GetDoctorIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerDoctorId != request.DoctorId)
            return AuthErrors.Forbidden;

        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
        {
            return DoctorErrors.DoctorNotFound;
        }

        return new DoctorProfileResult(
            doctor.Id,
            doctor.FirstName,
            doctor.LastName,
            doctor.LicenseNumber,
            doctor.Speciality,
            doctor.Email,
            doctor.Phone,
            doctor.IsActive,
            doctor.CreatedAt);
    }
}
