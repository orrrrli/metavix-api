using Application.UseCases.LabResult.Handlers;
using Application.UseCases.LabResult.Queries;

namespace Application.Tests.LabResults;

public class GetPatientLabResultsQueryHandlerTests
{
    private readonly ILabResultRepository _labResultRepository =
        Substitute.For<ILabResultRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetPatientLabResultsQueryHandler _handler;

    public GetPatientLabResultsQueryHandlerTests()
    {
        _handler = new GetPatientLabResultsQueryHandler(
            _labResultRepository,
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
        var records = new List<LabResult>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, SampleDate = new DateOnly(2026, 6, 1) },
            new() { Id = Guid.NewGuid(), PatientId = patientId, SampleDate = new DateOnly(2026, 7, 1) },
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _labResultRepository.GetAllByPatientIdAsync(patientId).Returns(records);

        var query = new GetPatientLabResultsQuery(patientId);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _handler.Handle(query, cts.Token);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        // The caller's token must be propagated to the load, not swallowed.
        await _patientRepository.Received(1)
            .GetOwnedPatientAsync(patientId, userId, cts.Token);
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
        _labResultRepository.GetAllByPatientIdAsync(patientId).Returns(new List<LabResult>());

        var query = new GetPatientLabResultsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert — no lab results yet is a valid empty result, not an error.
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
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

        var query = new GetPatientLabResultsQuery(patientId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _labResultRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetPatientLabResultsQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

}
