using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Handlers;
using Application.UseCases.DailyRecord.Queries;

namespace Application.Tests.DailyRecords;

public class GetBodyStatsQueryHandlerTests
{
    private readonly IDailyRecordRepository _dailyRecordRepository = Substitute.For<IDailyRecordRepository>();
    private readonly IPatientRepository     _patientRepository     = Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService    _currentUser           = Substitute.For<ICurrentUserService>();

    private readonly GetBodyStatsQueryHandler _handler;

    public GetBodyStatsQueryHandlerTests()
    {
        _handler = new GetBodyStatsQueryHandler(
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
        var patient   = new Patient { Id = patientId, UserId = userId, IsActive = true };
        var record    = BuildRecord(patientId, date, weightKg: 72.5m, waistCm: 88, createdAt: DateTime.UtcNow);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns(record);

        // Act
        ErrorOr<BodyStats> result =
            await _handler.Handle(new GetBodyStatsQuery(patientId, date), CancellationToken.None);

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
        var patient   = new Patient { Id = patientId, UserId = userId, IsActive = true };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns((DailyRecord?)null);

        // Act
        ErrorOr<BodyStats> result =
            await _handler.Handle(new GetBodyStatsQuery(patientId, date), CancellationToken.None);

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
        var patient   = new Patient { Id = patientId, UserId = userId, IsActive = true };
        var earliest  = BuildRecord(patientId, date, weightKg: 71.0m, waistCm: 85, createdAt: DateTime.UtcNow.AddHours(-3));

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository
            .GetFirstByPatientIdAndDateAsync(patientId, date, Arg.Any<CancellationToken>())
            .Returns(earliest);

        // Act
        ErrorOr<BodyStats> result =
            await _handler.Handle(new GetBodyStatsQuery(patientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WeightKg.Should().Be(71.0m);
        result.Value.WaistCm.Should().Be(85);
    }

    [Fact]
    public async Task Handle_WhenCallerPatientIdDoesNotMatchRequest_ReturnsForbidden()
    {
        // Arrange
        var userId         = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var date           = new DateOnly(2026, 6, 21);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(otherPatientId, userId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        ErrorOr<BodyStats> result =
            await _handler.Handle(new GetBodyStatsQuery(otherPatientId, date), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _dailyRecordRepository.DidNotReceive()
            .GetFirstByPatientIdAndDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        ErrorOr<BodyStats> result = await _handler.Handle(
            new GetBodyStatsQuery(Guid.NewGuid(), new DateOnly(2026, 6, 21)), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
