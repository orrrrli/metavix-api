using Application.UseCases.DailyRecord.Commands;
using Application.UseCases.DailyRecord.Handlers;

namespace Application.Tests.DailyRecords;

public class AddDailyRecordCommandHandlerTests
{
    private readonly IDailyRecordRepository _dailyRecordRepository =
        Substitute.For<IDailyRecordRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly AddDailyRecordCommandHandler _handler;

    public AddDailyRecordCommandHandlerTests()
    {
        _handler = new AddDailyRecordCommandHandler(
            _dailyRecordRepository,
            _patientRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenAllInputsValidAndPatientActive_PersistsAndReturnsResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _timeProvider.SetUtcNow(now);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            new TimeOnly(8, 30),
            120,
            80,
            72,
            75m,
            88,
            "Feeling good",
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PatientId.Should().Be(patientId);
        result.Value.SystolicPressure.Should().Be(120);
        result.Value.DiastolicPressure.Should().Be(80);
        result.Value.CreatedAt.Should().Be(now);
        await _dailyRecordRepository.Received(1).AddAsync(
            Arg.Is<DailyRecord>(r => r.PatientId == patientId && r.CreatedAt == now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSystolicProvidedWithoutDiastolic_ReturnsIncompleteBloodPressure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            120,
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Record.IncompleteBloodPressure");
        await _dailyRecordRepository.DidNotReceive()
            .AddAsync(Arg.Any<DailyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDiastolicProvidedWithoutSystolic_ReturnsIncompleteBloodPressure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            null,
            80,
            null,
            null,
            null,
            null,
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Record.IncompleteBloodPressure");
    }

    [Fact]
    public async Task Handle_WhenGlucoseValueIsZero_ReturnsInvalidValue()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var readings = new List<Application.UseCases.DailyRecord.Commands.GlucoseReading>
        {
            new(
                ReadingType: GlucoseReadingType.Fasting,
                ValueMgDl: 0,
                Time: new TimeOnly(7, 0),
                Foods: null,
                PostprandialWindow: null)
        };

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            readings);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("GlucoseReading.InvalidValue");
        await _dailyRecordRepository.DidNotReceive()
            .AddAsync(Arg.Any<DailyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGlucoseValueExceeds600_ReturnsInvalidValue()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var readings = new List<Application.UseCases.DailyRecord.Commands.GlucoseReading>
        {
            new(
                ReadingType: GlucoseReadingType.Fasting,
                ValueMgDl: 601,
                Time: new TimeOnly(7, 0),
                Foods: null,
                PostprandialWindow: null)
        };

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            readings);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("GlucoseReading.InvalidValue");
    }

    [Fact]
    public async Task Handle_WhenGlucoseReadingHasNoTime_ReturnsTimeRequired()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var readings = new List<Application.UseCases.DailyRecord.Commands.GlucoseReading>
        {
            new(
                ReadingType: GlucoseReadingType.PostBreakfast,
                ValueMgDl: 150,
                Time: null,
                Foods: "Toast",
                PostprandialWindow: PostprandialWindow.OneHour)
        };

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            readings);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("GlucoseReading.TimeRequired");
    }

    [Fact]
    public async Task Handle_WhenPatientIsInactive_ReturnsInactivePatient()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: false);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            120,
            80,
            null,
            null,
            null,
            null,
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Record.InactivePatient");
        await _dailyRecordRepository.DidNotReceive()
            .AddAsync(Arg.Any<DailyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new AddDailyRecordCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 20),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPatientNotOwnedByCaller_ReturnsForbidden()
    {
        // "Not found" and "not yours" both return null from the repository.
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            120,
            80,
            null,
            null,
            null,
            null,
            null);

        // Act
        ErrorOr<Application.UseCases.DailyRecord.Common.DailyRecordResult> result =
            await _handler.Handle(command, CancellationToken.None);

        // Assert — caller cannot distinguish "not found" from "not yours".
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _dailyRecordRepository.DidNotReceive()
            .AddAsync(Arg.Any<DailyRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPersisting_PropagatesCancellationTokenToAddAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            120,
            80,
            null,
            null,
            null,
            null,
            null);

        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        await _dailyRecordRepository.Received(1).AddAsync(
            Arg.Any<DailyRecord>(), cts.Token);
    }

    [Fact]
    public async Task Handle_WhenLoadingPatient_PropagatesCancellationTokenToGetOwnedPatientAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = BuildPatient(patientId, isActive: true);

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        var command = new AddDailyRecordCommand(
            patientId,
            new DateOnly(2026, 7, 20),
            null,
            120,
            80,
            null,
            null,
            null,
            null,
            null);

        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        await _patientRepository.Received(1).GetOwnedPatientAsync(patientId, userId, cts.Token);
    }

    private static Patient BuildPatient(Guid patientId, bool isActive) => new()
    {
        Id = patientId,
        UserId = Guid.NewGuid(),
        IsActive = isActive,
    };
}
