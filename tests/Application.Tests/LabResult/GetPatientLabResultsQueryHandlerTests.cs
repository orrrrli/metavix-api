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
        var patient = BuildPatient(patientId);
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

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
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

    private static Patient BuildPatient(Guid patientId) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = true,
    };
}
