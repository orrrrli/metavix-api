using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Handlers;
using Application.UseCases.DailyRecord.Queries;

namespace Application.Tests.DailyRecords;

public class GetDailyRecordSnapshotQueryHandlerTests
{
    private readonly IDailyRecordRepository _dailyRecordRepository = Substitute.For<IDailyRecordRepository>();
    private readonly IPatientRepository     _patientRepository     = Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService    _currentUser           = Substitute.For<ICurrentUserService>();

    private readonly GetDailyRecordSnapshotQueryHandler _handler;

    public GetDailyRecordSnapshotQueryHandlerTests()
    {
        _handler = new GetDailyRecordSnapshotQueryHandler(
            _dailyRecordRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenRecordExistsForDate_ReturnsWeightAndWaist()
    {
        // Arrange
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date      = new DateOnly(2026, 6, 21);
        var record    = BuildRecord(patientId, date, weightKg: 72.5m, waistCm: 88, createdAt: DateTime.UtcNow);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns(record);

        // Act
        ErrorOr<DailyRecordSnapshotResult> result =
            await _handler.Handle(new GetDailyRecordSnapshotQuery(patientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WeightKg.Should().Be(72.5m);
        result.Value.WaistCm.Should().Be(88);
    }

    [Fact]
    public async Task Handle_WhenNoRecordExistsForDate_ReturnsNullSnapshot()
    {
        // Arrange
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date      = new DateOnly(2026, 6, 21);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns((DailyRecord?)null);

        // Act
        ErrorOr<DailyRecordSnapshotResult> result =
            await _handler.Handle(new GetDailyRecordSnapshotQuery(patientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WeightKg.Should().BeNull();
        result.Value.WaistCm.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenMultipleRecordsSameDate_ReturnsEarliestCreatedAt()
    {
        // Arrange
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date      = new DateOnly(2026, 6, 21);
        var earliest  = BuildRecord(patientId, date, weightKg: 71.0m, waistCm: 85, createdAt: DateTime.UtcNow.AddHours(-3));

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns(earliest);

        // Act
        ErrorOr<DailyRecordSnapshotResult> result =
            await _handler.Handle(new GetDailyRecordSnapshotQuery(patientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WeightKg.Should().Be(71.0m);
        result.Value.WaistCm.Should().Be(85);
    }

    [Fact]
    public async Task Handle_WhenCallerPatientIdDoesNotMatchRequest_ReturnsForbidden()
    {
        // Arrange
        var userId          = Guid.NewGuid();
        var callerPatientId = Guid.NewGuid();
        var otherPatientId  = Guid.NewGuid();
        var date            = new DateOnly(2026, 6, 21);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(callerPatientId);

        // Act
        ErrorOr<DailyRecordSnapshotResult> result =
            await _handler.Handle(new GetDailyRecordSnapshotQuery(otherPatientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _dailyRecordRepository.DidNotReceive()
            .GetFirstByPatientIdAndDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    private static DailyRecord BuildRecord(
        Guid patientId,
        DateOnly date,
        decimal? weightKg,
        int? waistCm,
        DateTime createdAt) => new()
    {
        Id         = Guid.NewGuid(),
        PatientId  = patientId,
        RecordDate = date,
        WeightKg   = weightKg,
        WaistCm    = waistCm,
        CreatedAt  = createdAt,
    };
}
