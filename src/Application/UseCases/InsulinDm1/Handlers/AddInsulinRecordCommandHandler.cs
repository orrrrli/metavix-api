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
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddInsulinRecordCommandHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErrorOr<InsulinDm1RecordResult>> Handle(
        AddInsulinRecordCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return PatientErrors.PatientsNotFound;

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
            CreatedAt = _dateTimeProvider.UtcNow,
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
