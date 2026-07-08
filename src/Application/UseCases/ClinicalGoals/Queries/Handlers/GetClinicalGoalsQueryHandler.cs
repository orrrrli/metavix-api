using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.ClinicalGoals.Common;
using Application.UseCases.ClinicalGoals.Queries;

namespace Application.UseCases.ClinicalGoals.Handlers;

internal sealed class GetClinicalGoalsQueryHandler
    : IRequestHandler<GetClinicalGoalsQuery, ErrorOr<List<ClinicalGoalResult>>>
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public GetClinicalGoalsQueryHandler(
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

    public async Task<ErrorOr<List<ClinicalGoalResult>>> Handle(
        GetClinicalGoalsQuery request,
        CancellationToken cancellationToken)
    {
        var authError = await ClinicalGoalAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

        var goals = await _clinicalGoalRepository.GetByPatientIdAsync(request.PatientId);

        return goals.Select(ClinicalGoalMapper.ToResult).ToList();
    }
}
