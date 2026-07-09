using Application.UseCases.ClinicalGoals.Handlers;
using Application.UseCases.ClinicalGoals.Queries;

namespace Application.Tests.ClinicalGoals;

public class GetClinicalGoalsQueryHandlerTests
{
    private readonly IClinicalGoalRepository _clinicalGoalRepository = Substitute.For<IClinicalGoalRepository>();
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository = Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly GetClinicalGoalsQueryHandler _handler;

    public GetClinicalGoalsQueryHandlerTests()
    {
        _handler = new GetClinicalGoalsQueryHandler(
            _clinicalGoalRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    private void SetupAuth(Guid userId, Guid doctorId, Guid patientId)
    {
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenNoGoals_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns([]);

        var result = await _handler.Handle(new GetClinicalGoalsQuery(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Goals.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenGoalsExist_ReturnsMappedResults()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        SetupAuth(userId, doctorId, patientId);
        _clinicalGoalRepository.GetByPatientIdAsync(patientId).Returns(
        [
            new ClinicalGoal
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                ParameterId = "systolic_bp",
                CustomAtRiskHigh = 135m,
                CustomOutOfRangeHigh = 150m,
            }
        ]);

        var result = await _handler.Handle(new GetClinicalGoalsQuery(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Goals.Should().HaveCount(1);
        result.Value.Goals[0].ParameterId.Should().Be("systolic_bp");
        result.Value.Goals[0].CustomAtRiskHigh.Should().Be(135m);
    }

    [Fact]
    public async Task Handle_WhenNotLinked_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId).Returns(false);

        var result = await _handler.Handle(new GetClinicalGoalsQuery(doctorId, patientId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.Forbidden);
    }
}
