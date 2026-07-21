using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Mappers;

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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var patient = access.Value;

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

        return InsulinDm1RecordMapper.ToResult(record);
    }
}
