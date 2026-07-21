using Application.Common.Authorization;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Mappers;

namespace Application.UseCases.Doctor.Handlers;

internal sealed class GetLinkedPatientLabResultsQueryHandler
    : IRequestHandler<GetLinkedPatientLabResultsQuery, ErrorOr<List<LabResultResult>>>
{
    private readonly ILabResultRepository _labResultRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public GetLinkedPatientLabResultsQueryHandler(
        ILabResultRepository labResultRepository,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        ICurrentUserService currentUser)
    {
        _labResultRepository = labResultRepository;
        _doctorRepository = doctorRepository;
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<LabResultResult>>> Handle(
        GetLinkedPatientLabResultsQuery request,
        CancellationToken cancellationToken)
    {
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId, cancellationToken);
        if (authError is not null)
            return authError.Value;

        var records = await _labResultRepository.GetAllByPatientIdAsync(request.PatientId);

        // A linked patient with no lab results yet is a valid empty result,
        // not an error — mirrors GetPatientLabResultsQueryHandler.
        return records.Select(LabResultMapper.ToResult).ToList();
    }
}
