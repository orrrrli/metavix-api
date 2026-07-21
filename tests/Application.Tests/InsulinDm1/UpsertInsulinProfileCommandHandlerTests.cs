using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Handlers;

namespace Application.Tests.InsulinDm1;

public class UpsertInsulinProfileCommandHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly UpsertInsulinProfileCommandHandler _handler;

    public UpsertInsulinProfileCommandHandlerTests()
    {
        _handler = new UpsertInsulinProfileCommandHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndNoExistingProfile_CreatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _insulinRepository.GetProfileByPatientIdAsync(patientId).Returns((InsulinDm1Profile?)null);

        var command = new UpsertInsulinProfileCommand(
            patientId, "Humalog", 1.5m, 50, 100, "Dr. Smith", "555-1234");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InsulinName.Should().Be("Humalog");
        await _insulinRepository.Received(1).UpsertProfileAsync(
            Arg.Is<InsulinDm1Profile>(p => p.PatientId == patientId && p.InsulinName == "Humalog"));
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new UpsertInsulinProfileCommand(
            patientId, "Humalog", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().UpsertProfileAsync(Arg.Any<InsulinDm1Profile>());
    }

    [Fact]
    public async Task Handle_WhenPatientIsInactive_ReturnsInactivePatient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(BuildPatient(patientId, isActive: false));

        var command = new UpsertInsulinProfileCommand(
            patientId, "Humalog", null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(RecordErrors.InactivePatient.Code);
        await _insulinRepository.DidNotReceive().UpsertProfileAsync(Arg.Any<InsulinDm1Profile>());
    }

    private static Patient BuildPatient(Guid patientId, bool isActive = true) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = isActive,
    };
}
