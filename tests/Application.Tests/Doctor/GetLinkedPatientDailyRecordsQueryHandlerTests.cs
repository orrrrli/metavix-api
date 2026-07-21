using Application.UseCases.Doctor.Handlers;
using Application.UseCases.Doctor.Queries;

namespace Application.Tests.Doctors;

public class GetLinkedPatientDailyRecordsQueryHandlerTests
{
    private readonly IDailyRecordRepository _dailyRecordRepository =
        Substitute.For<IDailyRecordRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetLinkedPatientDailyRecordsQueryHandler _handler;

    public GetLinkedPatientDailyRecordsQueryHandlerTests()
    {
        _handler = new GetLinkedPatientDailyRecordsQueryHandler(
            _dailyRecordRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenLinkedAndHasRecords_ReturnsMappedRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var records = new List<DailyRecord>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, RecordDate = new DateOnly(2026, 6, 1) },
            new() { Id = Guid.NewGuid(), PatientId = patientId, RecordDate = new DateOnly(2026, 7, 1) },
        };

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns(records);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenPatientHasNoRecords_ReturnsEmptyListNotError()
    {
        // Arrange — a newly linked patient with no daily records yet is a
        // valid empty result, not RecordsNotFound.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheDoctor_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns((Doctor?)null);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        DoctorLinkSetup.Authorize(
            _currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId, linked: false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToAuthorizationChecks()
    {
        // Arrange — the caller's token must reach the repository calls, not be
        // swallowed and replaced with CancellationToken.None.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _dailyRecordRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), cts.Token);

        // Assert
        await _doctorRepository.Received(1).GetOwnedDoctorAsync(doctorId, userId, cts.Token);
        await _requestRepository.Received(1).IsAcceptedLinkAsync(doctorId, patientId, cts.Token);
    }
}
