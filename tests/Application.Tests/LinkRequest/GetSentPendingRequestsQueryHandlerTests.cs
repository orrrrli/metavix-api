using Application.UseCases.LinkRequest.Handlers;
using Application.UseCases.LinkRequest.Queries;

namespace Application.Tests.LinkRequest;

public class GetSentPendingRequestsQueryHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetSentPendingRequestsQueryHandler _handler;

    public GetSentPendingRequestsQueryHandlerTests()
    {
        _handler = new GetSentPendingRequestsQueryHandler(
            _requestRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndHasPendingRequests_ReturnsRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId);
        var pending = new List<PatientDoctorRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = Guid.NewGuid(),
                Status = RequestStatus.Pending,
                Doctor = new Doctor
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Ana",
                    PaternalLastName = "García",
                },
                CreatedAt = DateTime.UtcNow,
            },
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _requestRepository.GetPendingByPatientIdAsync(patientId).Returns(pending);

        var query = new GetSentPendingRequestsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetSentPendingRequestsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().GetPendingByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetSentPendingRequestsQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Patient BuildPatient(Guid patientId) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = true,
    };
}
