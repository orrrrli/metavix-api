using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LabResult.Commands;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Mappers;

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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var patient = access.Value;

        // 2. Guard — an inactive patient cannot record new data.
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

        return LabResultMapper.ToResult(record);
    }
}
