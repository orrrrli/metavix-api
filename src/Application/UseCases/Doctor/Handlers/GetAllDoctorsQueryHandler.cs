using Application.Common.Interfaces.Persistence;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetAllDoctorsQueryHandler
    : IRequestHandler<GetAllDoctorsQuery, ErrorOr<List<DoctorResult>>>
{
    private readonly IDoctorRepository _doctorRepository;

    public GetAllDoctorsQueryHandler(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    public async Task<ErrorOr<List<DoctorResult>>> Handle(
        GetAllDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var doctors = await _doctorRepository.GetAllActiveAsync();

        var results = doctors.Select(d => new DoctorResult(
            d.Id,
            d.FirstName,
            d.LastName,
            d.Speciality,
            d.Email)).ToList();

        return results;
    }
}
