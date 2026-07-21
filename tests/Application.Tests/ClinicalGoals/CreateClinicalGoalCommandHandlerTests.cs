using Application.Common.Constants;
using Application.UseCases.ClinicalGoals.Commands;
using Application.UseCases.ClinicalGoals.Handlers;
using Domain.Models;

namespace Application.Tests.ClinicalGoals;

public class CreateClinicalGoalCommandHandlerTests
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository = Substitute.For<IClinicalGoalRepository>();
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository = Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly CreateClinicalGoalCommandHandler _handler;

    public CreateClinicalGoalCommandHandlerTests()
    {
        _handler = new CreateClinicalGoalCommandHandler(
            _clinicalGoalRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    private CreateClinicalGoalCommand MakeCommand(Guid doctorId, Guid patientId, string parameterId = "systolic_bp") =>
        new(doctorId, patientId, parameterId, null, null, 135m, 150m);

    private void SetupAuth(Guid userId, Guid doctorId, Guid patientId, bool linked = true)
    {
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Doctor(doctorId, userId));
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId).Returns(linked);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesGoalAndReturnsResult()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        var result = await _handler.Handle(MakeCommand(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.ParameterId.Should().Be("systolic_bp");
        result.Value.CustomAtRiskHigh.Should().Be(135m);
        result.Value.CustomOutOfRangeHigh.Should().Be(150m);
        await _clinicalGoalRepository.Received(1).AddAsync(Arg.Is<ClinicalGoal>(g =>
            g.PatientId == patientId && g.DoctorId == doctorId && g.ParameterId == "systolic_bp"));
    }

    [Fact]
    public async Task Handle_WhenNotLinked_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId, linked: false);

        var result = await _handler.Handle(MakeCommand(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.Forbidden);
        await _clinicalGoalRepository.DidNotReceive().AddAsync(Arg.Any<ClinicalGoal>());
    }

    [Fact]
    public async Task Handle_WhenParameterUnknown_ReturnsUnknownParameter()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        var result = await _handler.Handle(
            MakeCommand(doctorId, patientId, "not_a_real_parameter"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.UnknownParameter);
    }

    [Fact]
    public async Task Handle_WhenGoalForParameterExists_ReturnsAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns(
        [
            new ClinicalGoal { Id = Guid.NewGuid(), PatientId = patientId, ParameterId = "systolic_bp" }
        ]);

        var result = await _handler.Handle(MakeCommand(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ClinicalGoalErrors.AlreadyExists);
        await _clinicalGoalRepository.DidNotReceive().AddAsync(Arg.Any<ClinicalGoal>());
    }

}
