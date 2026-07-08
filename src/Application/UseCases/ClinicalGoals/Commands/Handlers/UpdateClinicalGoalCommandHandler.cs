using Application.Common.Authorization;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Common;

namespace Application.UseCases.ClinicalGoals.Handlers;

internal sealed class UpdateClinicalGoalCommandHandler
    : IRequestHandler<UpdateClinicalGoalCommand, ErrorOr<ClinicalGoalResult>>
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdateClinicalGoalCommandHandler(
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

    public async Task<ErrorOr<ClinicalGoalResult>> Handle(
        UpdateClinicalGoalCommand request,
        CancellationToken cancellationToken)
    {
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

        var goal = await _clinicalGoalRepository.GetByIdAsync(request.GoalId);
        if (goal is null || goal.PatientId != request.PatientId || goal.DoctorId != request.DoctorId)
            return ClinicalGoalErrors.NotFound;

        goal.CustomOutOfRangeLow = request.CustomOutOfRangeLow;
        goal.CustomAtRiskLow = request.CustomAtRiskLow;
        goal.CustomAtRiskHigh = request.CustomAtRiskHigh;
        goal.CustomOutOfRangeHigh = request.CustomOutOfRangeHigh;

        await _clinicalGoalRepository.UpdateAsync(goal);

        return ClinicalGoalMapper.ToResult(goal);
    }
}
