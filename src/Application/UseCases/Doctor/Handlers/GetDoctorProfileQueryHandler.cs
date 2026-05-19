using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetDoctorProfileQueryHandler
    : IRequestHandler<GetDoctorProfileQuery, ErrorOr<DoctorProfileResult>>
{
    private readonly IDoctorRepository _doctorRepository;

    public GetDoctorProfileQueryHandler(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    public async Task<ErrorOr<DoctorProfileResult>> Handle(
        GetDoctorProfileQuery request,
        CancellationToken cancellationToken)
    {
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
