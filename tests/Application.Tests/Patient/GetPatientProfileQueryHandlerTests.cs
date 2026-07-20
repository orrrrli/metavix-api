using Application.UseCases.Patient.Handlers;
using Application.UseCases.Patient.Queries;

namespace Application.Tests.Patients;

public class GetPatientProfileQueryHandlerTests
{
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetPatientProfileQueryHandler _handler;

    public GetPatientProfileQueryHandlerTests()
    {
        _handler = new GetPatientProfileQueryHandler(
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwned_ReturnsProfile()
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
            Email = "juan@mail.com",
            IsActive = true,
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var query = new GetPatientProfileQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(patientId);
        result.Value.FirstName.Should().Be("Juan");
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetPatientProfileQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
    }
}
