using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Mappers;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetDoctorProfileByIdQueryHandler
    : IRequestHandler<GetDoctorProfileByIdQuery, ErrorOr<DoctorProfileResult>>
{
    private readonly IDoctorRepository _doctorRepository;

    public GetDoctorProfileByIdQueryHandler(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    public async Task<ErrorOr<DoctorProfileResult>> Handle(
        GetDoctorProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        if (doctor is null)
            return DoctorErrors.DoctorNotFound;

        return DoctorProfileMapper.ToResult(doctor);
    }
}
