using Application.UseCases.LabResult.Handlers;
using Application.UseCases.LabResult.Queries;

namespace Application.Tests.LabResults;

public class GetLabResultByIdQueryHandlerTests
{
    private readonly ILabResultRepository _labResultRepository =
        Substitute.For<ILabResultRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetLabResultByIdQueryHandler _handler;

    public GetLabResultByIdQueryHandlerTests()
    {
        _handler = new GetLabResultByIdQueryHandler(
            _labResultRepository,
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
        var record = new LabResult
        {
            Id = recordId,
            PatientId = patientId,
            SampleDate = new DateOnly(2026, 7, 1),
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _labResultRepository.GetByIdAsync(recordId).Returns(record);

        var query = new GetLabResultByIdQuery(patientId, recordId);

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
        // "Not found" and "not yours" both return null from the repository.
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var query = new GetLabResultByIdQuery(patientId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _labResultRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetLabResultByIdQuery(Guid.NewGuid(), Guid.NewGuid());

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
