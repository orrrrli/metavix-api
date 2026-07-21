using Application.UseCases.Doctor.Handlers;
using Application.UseCases.Doctor.Queries;

namespace Application.Tests.Doctors;

public class GetLinkedPatientLabResultsQueryHandlerTests
{
    private readonly ILabResultRepository _labResultRepository =
        Substitute.For<ILabResultRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetLinkedPatientLabResultsQueryHandler _handler;

    public GetLinkedPatientLabResultsQueryHandlerTests()
    {
        _handler = new GetLinkedPatientLabResultsQueryHandler(
            _labResultRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenLinkedAndHasResults_ReturnsMappedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var records = new List<LabResult>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, SampleDate = new DateOnly(2026, 6, 1) },
            new() { Id = Guid.NewGuid(), PatientId = patientId, SampleDate = new DateOnly(2026, 7, 1) },
        };

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _labResultRepository.GetAllByPatientIdAsync(patientId).Returns(records);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientLabResultsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenPatientHasNoResults_ReturnsEmptyListNotError()
    {
        // Arrange — a newly linked patient with no lab results yet is a
        // valid empty result, not RecordsNotFound.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _labResultRepository.GetAllByPatientIdAsync(patientId).Returns([]);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientLabResultsQuery(doctorId, patientId), CancellationToken.None);

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
            new GetLinkedPatientLabResultsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _labResultRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
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
            new GetLinkedPatientLabResultsQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _labResultRepository.DidNotReceive().GetAllByPatientIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToAuthorizationChecksNotToDataLoad()
    {
        // Arrange — the caller's token must reach the authorization checks, not
        // be swallowed and replaced with CancellationToken.None. It does NOT
        // reach GetAllByPatientIdAsync: ILabResultRepository doesn't accept a
        // token yet (see the TODO on the equivalent Unlink/Revoke call sites).
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _labResultRepository.GetAllByPatientIdAsync(patientId).Returns([]);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(new GetLinkedPatientLabResultsQuery(doctorId, patientId), cts.Token);

        // Assert
        await _doctorRepository.Received(1).GetOwnedDoctorAsync(doctorId, userId, cts.Token);
        await _requestRepository.Received(1).IsAcceptedLinkAsync(doctorId, patientId, cts.Token);
    }
}
