using Application.Common.Authorization;
using Application.Common.Constants;
using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Common;
using Domain.Models;

namespace Application.UseCases.ClinicalGoals.Handlers;

internal sealed class CreateClinicalGoalCommandHandler
    : IRequestHandler<CreateClinicalGoalCommand, ErrorOr<ClinicalGoalResult>>
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IPatientDoctorRequestRepository _requestRepository;
    private readonly ICurrentUserService _currentUser;

    public CreateClinicalGoalCommandHandler(
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
        CreateClinicalGoalCommand request,
        CancellationToken cancellationToken)
    {
        var authError = await DoctorPatientLinkAuth.AuthorizeAsync(
            _currentUser, _doctorRepository, _requestRepository, request.DoctorId, request.PatientId);
        if (authError is not null)
            return authError.Value;

        if (!AdaGoalConstants.KnownParameterIds.Contains(request.ParameterId))
            return ClinicalGoalErrors.UnknownParameter;

        var existing = await _clinicalGoalRepository.GetByPatientIdAsync(request.PatientId);
        if (existing.Any(g => g.ParameterId == request.ParameterId))
            return ClinicalGoalErrors.AlreadyExists;

        var goal = new ClinicalGoal
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ParameterId = request.ParameterId,
            CustomOutOfRangeLow = request.CustomOutOfRangeLow,
            CustomAtRiskLow = request.CustomAtRiskLow,
            CustomAtRiskHigh = request.CustomAtRiskHigh,
            CustomOutOfRangeHigh = request.CustomOutOfRangeHigh,
            CreatedAt = DateTime.UtcNow,
        };

        await _clinicalGoalRepository.AddAsync(goal);

        return ClinicalGoalMapper.ToResult(goal);
    }
}
