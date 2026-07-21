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
    public async Task Handle_WhenPatientHasNoRecords_ReturnsEmptyListNotError()
    {
        // Arrange — a newly linked patient with no daily records yet is a
        // valid empty result, not RecordsNotFound.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(true);
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
        result.FirstError.Code.Should().Be("Auth.Forbidden");
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientDailyRecordsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.Forbidden");
    }
}
