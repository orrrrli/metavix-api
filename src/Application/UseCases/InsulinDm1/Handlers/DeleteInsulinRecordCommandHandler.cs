using Application.Common.Authorization;
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
        // 1. Authenticate + load the owned patient (see PatientAccess).
        var access = await PatientAccess.RequireOwnedPatientAsync(
            _currentUser, _patientRepository, request.PatientId, cancellationToken);
        if (access.IsError)
            return access.Errors;

        var record = await _insulinRepository.GetRecordByIdAsync(request.RecordId);
        if (record is null || record.PatientId != request.PatientId)
            return InsulinDm1Errors.RecordNotFound;

        await _insulinRepository.DeleteRecordAsync(record);

        return Result.Deleted;
    }
}
