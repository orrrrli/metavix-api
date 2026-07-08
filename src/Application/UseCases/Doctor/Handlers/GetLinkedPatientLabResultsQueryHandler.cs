using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.LabResult.Common;

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
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

        var records = await _labResultRepository.GetAllByPatientIdAsync(request.PatientId);

        var results = records.Select(r => new LabResultResult(
            r.Id,
            r.PatientId,
            r.SampleDate,
            r.Hba1c,
            r.TotalCholesterol,
            r.Ldl,
            r.Hdl,
            r.Triglycerides,
            r.Creatinine,
            r.Bun,
            r.EgoProteins,
            r.EgoGlucose,
            r.Notes,
            r.CreatedAt)).ToList();

        if (results.Count == 0)
            return RecordErrors.RecordsNotFound;

        return results;
    }
}
