using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.InsulinDm1.Commands;

namespace Application.UseCases.InsulinDm1.Handlers;

internal sealed class DeleteInsulinRecordCommandHandler
    : IRequestHandler<DeleteInsulinRecordCommand, ErrorOr<Deleted>>
{
    private readonly IInsulinDm1Repository _insulinRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ICurrentUserService _currentUser;

    public DeleteInsulinRecordCommandHandler(
        IInsulinDm1Repository insulinRepository,
        IPatientRepository patientRepository,
        ICurrentUserService currentUser)
    {
        _insulinRepository = insulinRepository;
        _patientRepository = patientRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Deleted>> Handle(
        DeleteInsulinRecordCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var record = await _insulinRepository.GetRecordByIdAsync(request.RecordId);
        if (record is null || record.PatientId != request.PatientId)
            return InsulinDm1Errors.RecordNotFound;

        await _insulinRepository.DeleteRecordAsync(record);

        return Result.Deleted;
    }
}
