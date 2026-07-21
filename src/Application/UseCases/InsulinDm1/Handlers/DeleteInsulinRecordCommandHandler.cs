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

        var record = await _insulinRepository.GetRecordByIdAsync(request.RecordId);
        if (record is null || record.PatientId != request.PatientId)
            return InsulinDm1Errors.RecordNotFound;

        await _insulinRepository.DeleteRecordAsync(record);

        return Result.Deleted;
    }
}
