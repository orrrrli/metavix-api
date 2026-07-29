using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Handlers;

internal sealed class RejectLinkRequestCommandHandler
    : IRequestHandler<RejectLinkRequestCommand, ErrorOr<LinkRequestResult>>
{
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RejectLinkRequestCommandHandler(
        IPatientDoctorRequestRepository requestRepository,
        IDoctorRepository doctorRepository,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _requestRepository = requestRepository;
        _doctorRepository = doctorRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<LinkRequestResult>> Handle(
        RejectLinkRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserAccess.RequireUserId(_currentUser, out var userId) is { } userIdError)
            return userIdError;

        // 1. Find the link request
        var linkRequest = await _requestRepository.GetByIdAsync(request.RequestId);
        if (linkRequest is null)
        {
            return LinkRequestErrors.RequestNotFound;
        }

        var callerDoctor = await _doctorRepository.GetOwnedDoctorAsync(
            linkRequest.DoctorId, userId, cancellationToken);
        if (callerDoctor is null)
            return AuthErrors.Forbidden;

        // 2. Reject the request (fails if not pending), then flush to detect
        //    a concurrent transition that already won the race.
        if (!linkRequest.Reject(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return LinkRequestErrors.NotPending;
        }
        _requestRepository.MarkForUpdate(linkRequest);
        try
        {
            await _unitOfWork.FlushAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return LinkRequestErrors.NotPending;
        }

        return new LinkRequestResult(
            linkRequest.Id,
            linkRequest.PatientId,
            linkRequest.DoctorId,
            linkRequest.Status.ToString(),
            linkRequest.CreatedAt);
    }
}
