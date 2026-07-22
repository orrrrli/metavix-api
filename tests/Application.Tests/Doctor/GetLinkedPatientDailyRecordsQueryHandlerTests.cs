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
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
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
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
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
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        DoctorLinkSetup.Authorize(
            _currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId, doctorOwned: false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().IsAcceptedLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _dailyRecordRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        DoctorLinkSetup.Authorize(
            _currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId, linked: false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.Received(1).IsAcceptedLinkAsync(
            doctorId, patientId, Arg.Any<CancellationToken>());
        await _dailyRecordRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToAuthorizationChecks()
    {
        // Arrange — the caller's token must reach the authorization checks, not
        // be swallowed and replaced with CancellationToken.None. GetAllByPatientIdAsync
        // is untested here for token propagation: IDailyRecordRepository's signature
        // doesn't accept one yet (see the TODO on the equivalent Unlink/Revoke call sites).
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
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
