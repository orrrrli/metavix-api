using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LabResult.Commands;
using Application.UseCases.LabResult.Common;

namespace Application.UseCases.LabResult.Handlers;

internal sealed class AddLabResultCommandHandler
    : IRequestHandler<AddLabResultCommand, ErrorOr<LabResultResult>>
{
    private readonly ILabResultRepository _labResultRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddLabResultCommandHandler(
        ILabResultRepository labResultRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _labResultRepository = labResultRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<LabResultResult>> Handle(
        AddLabResultCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize
        if (_currentUser.UserId is not { } userId)
            return AuthErrors.Forbidden;

        // 2. Load — single query resolves ownership + existence.
        //    "Not found" and "not yours" are collapsed into Forbidden to
        //    close the patient-ID enumeration oracle.
        var patient = await _patientRepository.GetOwnedPatientAsync(
            request.PatientId, userId, cancellationToken);
        if (patient is null)
            return AuthErrors.Forbidden;

        // 3. Guard — an inactive patient cannot record new data.
        if (!patient.IsActive)
            return RecordErrors.InactivePatient;

        var record = new Domain.Models.LabResult
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            SampleDate = request.SampleDate,
            Hba1c = request.Hba1c,
            TotalCholesterol = request.TotalCholesterol,
            Ldl = request.Ldl,
            Hdl = request.Hdl,
            Triglycerides = request.Triglycerides,
            Creatinine = request.Creatinine,
            Bun = request.Bun,
            EgoProteins = request.EgoProteins,
            EgoGlucose = request.EgoGlucose,
            Notes = request.Notes,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _labResultRepository.AddAsync(record);

        return new LabResultResult(
            record.Id,
            record.PatientId,
            record.SampleDate,
            record.Hba1c,
            record.TotalCholesterol,
            record.Ldl,
            record.Hdl,
            record.Triglycerides,
            record.Creatinine,
            record.Bun,
            record.EgoProteins,
            record.EgoGlucose,
            record.Notes,
            record.CreatedAt);
    }
}
