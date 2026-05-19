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
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddLabResultCommandHandler(
        ILabResultRepository labResultRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _labResultRepository = labResultRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<LabResultResult>> Handle(
        AddLabResultCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
        {
            return PatientErrors.PatientsNotFound;
        }

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
            CreatedAt = _dateTimeProvider.UtcNow
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
