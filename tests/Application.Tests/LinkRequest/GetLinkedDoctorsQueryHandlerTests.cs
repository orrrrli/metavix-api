using Application.UseCases.LinkRequest.Handlers;
using Application.UseCases.LinkRequest.Queries;

namespace Application.Tests.LinkRequest;

public class GetLinkedDoctorsQueryHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetLinkedDoctorsQueryHandler _handler;

    public GetLinkedDoctorsQueryHandlerTests()
    {
        _handler = new GetLinkedDoctorsQueryHandler(
            _requestRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndHasAcceptedRequests_ReturnsDoctors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patient = TestEntities.Patient(patientId);
        var accepted = new List<PatientDoctorRequest>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                Status = RequestStatus.Accepted,
                Doctor = new Doctor
                {
                    Id = doctorId,
                    FirstName = "Ana",
                    PaternalLastName = "García",
                    Email = "ana@mail.com",
                },
                ResolvedAt = DateTime.UtcNow,
            },
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _requestRepository.GetAcceptedByPatientIdAsync(patientId).Returns(accepted);

        var query = new GetLinkedDoctorsQuery(patientId);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _handler.Handle(query, cts.Token);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        await _patientRepository.Received(1)
            .GetOwnedPatientAsync(patientId, userId, cts.Token);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedButHasNoAcceptedRequests_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Patient(patientId));
        _requestRepository.GetAcceptedByPatientIdAsync(patientId)
            .Returns(new List<PatientDoctorRequest>());

        var query = new GetLinkedDoctorsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — no linked doctors is a valid empty result, not an error.
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetLinkedDoctorsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().GetAcceptedByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetLinkedDoctorsQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

}
