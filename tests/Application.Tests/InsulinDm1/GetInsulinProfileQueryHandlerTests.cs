using Application.UseCases.InsulinDm1.Handlers;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.Tests.InsulinDm1;

public class GetInsulinProfileQueryHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetInsulinProfileQueryHandler _handler;

    public GetInsulinProfileQueryHandlerTests()
    {
        _handler = new GetInsulinProfileQueryHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndProfileExists_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId);
        var profile = new InsulinDm1Profile
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            InsulinName = "Humalog",
            Ric = 1.5m,
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _insulinRepository.GetProfileByPatientIdAsync(patientId).Returns(profile);

        var query = new GetInsulinProfileQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InsulinName.Should().Be("Humalog");
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetInsulinProfileQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().GetProfileByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetInsulinProfileQuery(Guid.NewGuid());

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
