using Application.UseCases.Doctor.Handlers;
using Application.UseCases.Doctor.Queries;

namespace Application.Tests.Doctors;

public class GetLinkedPatientProfileQueryHandlerTests
{
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetLinkedPatientProfileQueryHandler _handler;

    public GetLinkedPatientProfileQueryHandlerTests()
    {
        _handler = new GetLinkedPatientProfileQueryHandler(
            _patientRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenLinkedAndPatientExists_ReturnsMappedProfile()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        var patient = TestEntities.Patient(patientId, userId: userId);

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientProfileQuery(DoctorId: doctorId, PatientId: patientId), CancellationToken.None);

        // Assert — assert a couple of mapped fields, not the whole record, so the
        // test stays resilient to future mapper additions. A mapper that returned
        // all-defaults would still pass on the Id check alone.
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(patientId);
        result.Value.FirstName.Should().Be("Juan");
        result.Value.LastName.Should().Be("Pérez");
    }

    [Fact]
    public async Task Handle_WhenLinkedButPatientRecordMissing_ReturnsPatientNotFound()
    {
        // Arrange — post-#263, the sibling handler returns PatientNotFound
        // honestly for a since-deleted patient record (not Forbidden), because
        // DoctorPatientLinkAuth has already closed the enumeration oracle at
        // the auth step. This test pins that contract so a future refactor
        // can't silently "fix" it back to Forbidden.
        var (userId, doctorId, patientId) = TestIds.DoctorLink();

        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        _patientRepository.GetByIdAsync(patientId).Returns((Patient?)null);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientProfileQuery(DoctorId: doctorId, PatientId: patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PatientNotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheDoctor_ReturnsForbidden()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        var otherDoctorId = Guid.NewGuid();
        DoctorLinkSetup.Authorize(
            _currentUser, _doctorRepository, _requestRepository,
            userId, otherDoctorId, patientId, doctorOwned: false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientProfileQuery(DoctorId: otherDoctorId, PatientId: patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.Received(1).GetOwnedDoctorAsync(
            otherDoctorId, userId, Arg.Any<CancellationToken>());
        await _requestRepository.DidNotReceive().IsAcceptedLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _patientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        DoctorLinkSetup.Authorize(
            _currentUser, _doctorRepository, _requestRepository,
            userId, doctorId, patientId, linked: false);

        // Act
        var result = await _handler.Handle(
            new GetLinkedPatientProfileQuery(DoctorId: doctorId, PatientId: patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.Received(1).IsAcceptedLinkAsync(
            doctorId, patientId, Arg.Any<CancellationToken>());
        await _patientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToAuthorizationChecks()
    {
        // Arrange — the caller's token must reach the authorization checks, not
        // be swallowed and replaced with CancellationToken.None. GetByIdAsync is
        // untested here for token propagation: IPatientRepository.GetByIdAsync
        // doesn't accept a CT yet (see the remarks block on IPatientRepository).
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        DoctorLinkSetup.Authorize(_currentUser, _doctorRepository, _requestRepository, userId, doctorId, patientId);
        // Stubbed to short-circuit the load; mapper would NRE on null.
        _patientRepository.GetByIdAsync(patientId).Returns((Patient?)null);
        using var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(new GetLinkedPatientProfileQuery(DoctorId: doctorId, PatientId: patientId), cts.Token);

        // Assert
        await _doctorRepository.Received(1).GetOwnedDoctorAsync(doctorId, userId, cts.Token);
        await _requestRepository.Received(1).IsAcceptedLinkAsync(doctorId, patientId, cts.Token);
        await _patientRepository.Received(1).GetByIdAsync(patientId);
    }
}
