using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class AddInsulinRecordCommandHandler
    : IRequestHandler<AddInsulinRecordCommand, ErrorOr<InsulinDm1RecordResult>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddInsulinRecordCommandHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<InsulinDm1RecordResult>> Handle(
        AddInsulinRecordCommand request,
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

        var record = new Domain.Models.InsulinDm1Record
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            RecordDate = request.RecordDate,
            GlucoseBefore = request.GlucoseBefore,
            GlucoseAfter = request.GlucoseAfter,
            TotalCarbs = request.TotalCarbs,
            DoseApplied = request.DoseApplied,
            MealDescription = request.MealDescription,
            HowIFelt = request.HowIFelt,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        await _insulinRepository.AddRecordAsync(record);

        return new InsulinDm1RecordResult(
            record.Id,
            record.PatientId,
            record.RecordDate,
            record.GlucoseBefore,
            record.GlucoseAfter,
            record.TotalCarbs,
            record.DoseApplied,
            record.MealDescription,
            record.HowIFelt,
            record.CreatedAt);
    }
}
