using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Handlers;
using Domain.Models;

namespace Application.Tests.ClinicalGoals;

public class UpdateClinicalGoalCommandHandlerTests
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository = Substitute.For<IClinicalGoalRepository>();
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository = Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly UpdateClinicalGoalCommandHandler _handler;

    public UpdateClinicalGoalCommandHandlerTests()
    {
        _handler = new UpdateClinicalGoalCommandHandler(
            _clinicalGoalRepository, _doctorRepository, _requestRepository, _currentUser);
    }


    [Fact]
    public async Task Handle_WhenGoalExists_UpdatesThresholds()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);

        var goal = new ClinicalGoal
        {
            Id = goalId,
            PatientId = patientId,
            DoctorId = doctorId,
            ParameterId = "systolic_bp",
            CustomAtRiskHigh = 135m,
            CustomOutOfRangeHigh = 150m,
        };
        _clinicalGoalRepository.GetOwnedAsync(goalId, patientId, doctorId).Returns(goal);

        var command = new UpdateClinicalGoalCommand(doctorId, patientId, goalId, null, null, 130m, 145m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.CustomAtRiskHigh.Should().Be(130m);
        result.Value.CustomOutOfRangeHigh.Should().Be(145m);
        await _clinicalGoalRepository.Received(1).UpdateAsync(Arg.Is<ClinicalGoal>(g => g.Id == goalId));
    }

    [Fact]
    public async Task Handle_WhenGoalNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _clinicalGoalRepository.GetOwnedAsync(goalId, patientId, doctorId).Returns((ClinicalGoal?)null);

        var command = new UpdateClinicalGoalCommand(doctorId, patientId, goalId, null, null, 130m, 145m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.NotFound);
    }

    // The repository's GetOwnedAsync filters by patient and doctor in the query itself, so a
    // goal belonging to another patient (or another doctor, tested below) simply isn't returned.
    [Fact]
    public async Task Handle_WhenGoalBelongsToAnotherPatient_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _clinicalGoalRepository.GetOwnedAsync(goalId, patientId, doctorId).Returns((ClinicalGoal?)null);

        var command = new UpdateClinicalGoalCommand(doctorId, patientId, goalId, null, null, 130m, 145m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.NotFound);
        await _clinicalGoalRepository.DidNotReceive().UpdateAsync(Arg.Any<ClinicalGoal>());
    }

    // Regression: a linked doctor other than the one who created the goal must not be able to
    // update it, even though both are validly linked to the same patient.
    [Fact]
    public async Task Handle_WhenGoalBelongsToAnotherDoctor_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _clinicalGoalRepository.GetOwnedAsync(goalId, patientId, doctorId).Returns((ClinicalGoal?)null);

        var command = new UpdateClinicalGoalCommand(doctorId, patientId, goalId, null, null, 130m, 145m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.NotFound);
        await _clinicalGoalRepository.DidNotReceive().UpdateAsync(Arg.Any<ClinicalGoal>());
    }

}
