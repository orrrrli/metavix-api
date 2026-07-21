using Application.UseCases.Patient.Handlers;
using Application.UseCases.Patient.Queries;

namespace Application.Tests.Patients;

public class GetMyPatientProfileQueryHandlerTests
{
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetMyPatientProfileQueryHandler _handler;

    public GetMyPatientProfileQueryHandlerTests()
    {
        _handler = new GetMyPatientProfileQueryHandler(
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCallerHasPatient_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            UserId = userId,
            FirstName = "Juan",
            LastName = "Pérez",
            IsActive = true,
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var query = new GetMyPatientProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(patientId);
        result.Value.FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoPatient_ReturnsPatientNotFound()
    {
        var userId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetMyPatientProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PatientNotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetMyPatientProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
