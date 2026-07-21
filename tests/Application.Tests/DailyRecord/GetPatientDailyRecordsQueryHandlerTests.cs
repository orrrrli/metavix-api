using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Handlers;
using Application.UseCases.DailyRecord.Queries;

namespace Application.Tests.DailyRecords;

public class GetPatientDailyRecordsQueryHandlerTests
{
    private readonly IDailyRecordRepository _dailyRecordRepository = Substitute.For<IDailyRecordRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly GetPatientDailyRecordsQueryHandler _handler;

    public GetPatientDailyRecordsQueryHandlerTests()
    {
        _handler = new GetPatientDailyRecordsQueryHandler(
            _dailyRecordRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenDateFromAndDateToAreNull_ReturnsAllRecordsInDescendingDateOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId };

        var records = new List<DailyRecord>
        {
            BuildRecord(patientId, new DateOnly(2026, 6, 20)),
            BuildRecord(patientId, new DateOnly(2026, 6, 15)),
            BuildRecord(patientId, new DateOnly(2026, 6, 10)),
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns(records);

        // Act
        ErrorOr<List<DailyRecordResult>> result =
            await _handler.Handle(new GetPatientDailyRecordsQuery(patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Select(r => r.RecordDate)
            .Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Handle_WhenDateRangeProvided_ReturnsOnlyRecordsWithinInclusiveRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId };
        var dateFrom = new DateOnly(2026, 6, 12);
        var dateTo = new DateOnly(2026, 6, 18);

        var recordsInRange = new List<DailyRecord>
        {
            BuildRecord(patientId, new DateOnly(2026, 6, 12)),
            BuildRecord(patientId, new DateOnly(2026, 6, 18)),
            BuildRecord(patientId, new DateOnly(2026, 6, 15)),
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository
            .GetByPatientIdInRangeAsync(patientId, dateFrom, dateTo, Arg.Any<CancellationToken>())
            .Returns(recordsInRange);

        // Act
        ErrorOr<List<DailyRecordResult>> result =
            await _handler.Handle(new GetPatientDailyRecordsQuery(patientId, dateFrom, dateTo), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(r => r.RecordDate >= dateFrom && r.RecordDate <= dateTo);
        await _dailyRecordRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenDateRangeContainsNoRecords_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, UserId = userId };
        var dateFrom = new DateOnly(2026, 6, 1);
        var dateTo = new DateOnly(2026, 6, 5);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>()).Returns(patient);
        _dailyRecordRepository
            .GetByPatientIdInRangeAsync(patientId, dateFrom, dateTo, Arg.Any<CancellationToken>())
            .Returns(new List<DailyRecord>());

        // Act
        ErrorOr<List<DailyRecordResult>> result =
            await _handler.Handle(new GetPatientDailyRecordsQuery(patientId, dateFrom, dateTo), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCallerPatientIdDoesNotMatchRequest_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(otherPatientId, userId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        ErrorOr<List<DailyRecordResult>> result =
            await _handler.Handle(new GetPatientDailyRecordsQuery(otherPatientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _dailyRecordRepository.DidNotReceive()
            .GetAllByPatientIdAsync(Arg.Any<Guid>());
        await _dailyRecordRepository.DidNotReceive()
            .GetByPatientIdInRangeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(
            new GetPatientDailyRecordsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static DailyRecord BuildRecord(Guid patientId, DateOnly recordDate) => new()
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        RecordDate = recordDate,
        RecordTime = new TimeOnly(8, 0),
        SystolicPressure = 120,
        DiastolicPressure = 80,
        HeartRate = 70,
        WeightKg = 70m,
        WaistCm = 85,
        Notes = "Test record",
        CreatedAt = DateTime.UtcNow,
        GlucoseReadings = [],
    };
}
