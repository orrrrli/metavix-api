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
        var patient = BuildPatient(patientId);
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

        var query = new GetLinkedDoctorsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().GetAcceptedByPatientIdAsync(Arg.Any<Guid>());
    }

    private static Patient BuildPatient(Guid patientId) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = true,
    };
}
