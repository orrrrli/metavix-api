using Application.UseCases.InsulinDm1.Handlers;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.Tests.InsulinDm1;

public class GetInsulinRecordsQueryHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetInsulinRecordsQueryHandler _handler;

    public GetInsulinRecordsQueryHandlerTests()
    {
        _handler = new GetInsulinRecordsQueryHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndHasRecords_ReturnsRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = TestEntities.Patient(patientId);
        var records = new List<InsulinDm1Record>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, RecordDate = new DateOnly(2026, 7, 1) },
            new() { Id = Guid.NewGuid(), PatientId = patientId, RecordDate = new DateOnly(2026, 7, 2) },
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _insulinRepository.GetRecordsByPatientIdAsync(patientId).Returns(records);

        var query = new GetInsulinRecordsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedButHasNoRecords_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Patient(patientId));
        _insulinRepository.GetRecordsByPatientIdAsync(patientId)
            .Returns(new List<InsulinDm1Record>());

        var query = new GetInsulinRecordsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — no insulin records yet is a valid empty result, not an error.
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

        var query = new GetInsulinRecordsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().GetRecordsByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetInsulinRecordsQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

}
