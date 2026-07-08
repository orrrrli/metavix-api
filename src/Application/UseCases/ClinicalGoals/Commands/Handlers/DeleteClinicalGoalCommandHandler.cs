using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Common;

namespace Application.UseCases.ClinicalGoals.Handlers;

internal sealed class DeleteClinicalGoalCommandHandler
    : IRequestHandler<DeleteClinicalGoalCommand, ErrorOr<Deleted>>
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public DeleteClinicalGoalCommandHandler(
        IClinicalGoalRepository clinicalGoalRepository,
        IDoctorRepository doctorRepository,
        IPatientDoctorRequestRepository requestRepository,
        ICurrentUserService currentUser)
    {
        _clinicalGoalRepository = clinicalGoalRepository;
        _doctorRepository = doctorRepository;
        _requestRepository = requestRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Deleted>> Handle(
        DeleteClinicalGoalCommand request,
        CancellationToken cancellationToken)
    {
        var authError = await ClinicalGoalAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

        var goal = await _clinicalGoalRepository.GetByIdAsync(request.GoalId);
        if (goal is null || goal.PatientId != request.PatientId)
            return ClinicalGoalErrors.NotFound;

        await _clinicalGoalRepository.DeleteAsync(goal);

        return Result.Deleted;
    }
}
