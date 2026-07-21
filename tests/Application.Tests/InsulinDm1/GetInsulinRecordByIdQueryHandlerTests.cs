using Application.UseCases.InsulinDm1.Handlers;
using Application.UseCases.InsulinDm1.Queries;

namespace Application.Tests.InsulinDm1;

public class GetInsulinRecordByIdQueryHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetInsulinRecordByIdQueryHandler _handler;

    public GetInsulinRecordByIdQueryHandlerTests()
    {
        _handler = new GetInsulinRecordByIdQueryHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndRecordBelongsToPatient_ReturnsRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var patient = BuildPatient(patientId);
        var record = new InsulinDm1Record
        {
            Id = recordId,
            PatientId = patientId,
            RecordDate = new DateOnly(2026, 7, 1),
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _insulinRepository.GetRecordByIdAsync(recordId).Returns(record);

        var query = new GetInsulinRecordByIdQuery(patientId, recordId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(recordId);
        result.Value.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetInsulinRecordByIdQuery(patientId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().GetRecordByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetInsulinRecordByIdQuery(Guid.NewGuid(), Guid.NewGuid());

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
