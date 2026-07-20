using Application.UseCases.Patient.Handlers;
using Application.UseCases.Patient.Queries;

namespace Application.Tests.Patients;

public class GetPatientResumenQueryHandlerTests
{
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly IDailyRecordRepository _dailyRecordRepository =
        Substitute.For<IDailyRecordRepository>();
    private readonly ILabResultRepository _labResultRepository =
        Substitute.For<ILabResultRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetPatientResumenQueryHandler _handler;

    public GetPatientResumenQueryHandlerTests()
    {
        _handler = new GetPatientResumenQueryHandler(
            _patientRepository,
            _dailyRecordRepository,
            _labResultRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwned_ReturnsResumen()
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
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _dailyRecordRepository.GetLatestByPatientIdAsync(patientId).Returns((DailyRecord?)null);
        _labResultRepository.GetLatestByPatientIdAsync(patientId).Returns((LabResult?)null);

        var query = new GetPatientResumenQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Perfil.Nombre.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetPatientResumenQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _dailyRecordRepository.DidNotReceive().GetLatestByPatientIdAsync(Arg.Any<Guid>());
        await _labResultRepository.DidNotReceive().GetLatestByPatientIdAsync(Arg.Any<Guid>());
    }
}
