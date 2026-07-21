using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class GetInsulinRecordsQueryHandler
    : IRequestHandler<GetInsulinRecordsQuery, ErrorOr<List<InsulinDm1RecordResult>>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public GetInsulinRecordsQueryHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<List<InsulinDm1RecordResult>>> Handle(
        GetInsulinRecordsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var records = await _insulinRepository.GetRecordsByPatientIdAsync(request.PatientId);

        // 3. Map — an owned patient with no insulin records yet is a valid empty
        //    result, not an error. Returning RecordsNotFound would force callers
        //    to treat "no records yet" as a failure.
        return records.Select(r => new InsulinDm1RecordResult(
            r.Id,
            r.PatientId,
            r.RecordDate,
            r.GlucoseBefore,
            r.GlucoseAfter,
            r.TotalCarbs,
            r.DoseApplied,
            r.MealDescription,
            r.HowIFelt,
            r.CreatedAt)).ToList();
    }
}
