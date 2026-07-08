using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Handlers;

namespace Application.Tests.ClinicalGoals;

public class DeleteClinicalGoalCommandHandlerTests
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository = Substitute.For<IClinicalGoalRepository>();
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository = Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly DeleteClinicalGoalCommandHandler _handler;

    public DeleteClinicalGoalCommandHandlerTests()
    {
        _handler = new DeleteClinicalGoalCommandHandler(
            _clinicalGoalRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    private void SetupAuth(Guid userId, Guid doctorId, Guid patientId)
    {
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenGoalExists_DeletesGoal()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);

        var goal = new ClinicalGoal { Id = goalId, PatientId = patientId, DoctorId = doctorId, ParameterId = "systolic_bp" };
        _clinicalGoalRepository.GetByIdAsync(goalId).Returns(goal);

        var result = await _handler.Handle(
            new DeleteClinicalGoalCommand(doctorId, patientId, goalId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _clinicalGoalRepository.Received(1).DeleteAsync(goal);
    }

    [Fact]
    public async Task Handle_WhenGoalNotFound_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByIdAsync(goalId).Returns((ClinicalGoal?)null);

        var result = await _handler.Handle(
            new DeleteClinicalGoalCommand(doctorId, patientId, goalId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.NotFound);
        await _clinicalGoalRepository.DidNotReceive().DeleteAsync(Arg.Any<ClinicalGoal>());
    }

    // Regression: a linked doctor other than the one who created the goal must not be able to
    // delete it, even though both are validly linked to the same patient.
    [Fact]
    public async Task Handle_WhenGoalBelongsToAnotherDoctor_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);

        var goal = new ClinicalGoal
        {
            Id = goalId,
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ParameterId = "systolic_bp",
        };
        _clinicalGoalRepository.GetByIdAsync(goalId).Returns(goal);

        var result = await _handler.Handle(
            new DeleteClinicalGoalCommand(doctorId, patientId, goalId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.NotFound);
        await _clinicalGoalRepository.DidNotReceive().DeleteAsync(Arg.Any<ClinicalGoal>());
    }
}
