using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Handlers;
using Domain.Models;

namespace Application.Tests.LinkRequest;

public class UnlinkPatientCommandHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly UnlinkPatientCommandHandler _handler;

    public UnlinkPatientCommandHandlerTests()
    {
        _handler = new UnlinkPatientCommandHandler(
            _requestRepository,
            _patientRepository,
            _doctorRepository,
            _currentUser,
            _timeProvider);

        _requestRepository.UpdateAsync(Arg.Any<PatientDoctorRequest>()).Returns(true);
    }

    [Fact]
    public async Task Handle_WhenPatientHadMrn_ClearsItOnUnlink()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000042";
        var now = DateTime.UtcNow;

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);
        var patient = TestEntities.Patient(patientId, primaryDoctorId: doctorId, medicalRecordNumber: mrn);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _timeProvider.SetUtcNow(now);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Unlinked");
        await _requestRepository.Received(1).UpdateAsync(Arg.Is<PatientDoctorRequest>(r =>
            r.Status == RequestStatus.Unlinked && r.ResolvedAt == now));
        await _patientRepository.Received(1).UpdateAsync(Arg.Is<Patient>(p =>
            p.PrimaryDoctorId == null &&
            p.MedicalRecordNumber == null));
    }

    [Fact]
    public async Task Handle_WhenNotAccepted_ReturnsNotAcceptedWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Pending);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(LinkRequestErrors.NotAccepted.Code);
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheDoctor_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);

        _currentUser.UserId.Returns(userId);
        // No doctor with this id belongs to userId → GetOwnedDoctorAsync returns null.
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns((Doctor?)null);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

    [Fact]
    public async Task Handle_WhenRequestNotFound_ReturnsForbiddenNotRequestNotFound()
    {
        // Arrange — an unknown requestId must be indistinguishable from a
        // request that exists but isn't the caller's doctor, so no requestId
        // enumeration oracle leaks.
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns((PatientDoctorRequest?)null);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        // A reordered handler that checked doctor ownership before the request
        // even existed would still pass a looser assertion; pin the short-circuit.
        await _doctorRepository.DidNotReceive().GetOwnedDoctorAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

    [Fact]
    public async Task Handle_WhenPatientDeletedAfterRequestLoaded_SucceedsWithoutDetaching()
    {
        // Arrange — the link request still exists, but the patient it points
        // at has been deleted before the unlink ran, so GetByIdAsync (step 4)
        // yields null. Unlike RevokeDoctorAccessCommandHandler — which loads
        // the patient during authorization and fails Forbidden if it's
        // gone — Unlink authorizes against the doctor and only touches the
        // patient afterwards, so the request transition still succeeds; there
        // is simply nothing left to detach.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns((Patient?)null);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert — pins the actual state transition, not just "some UpdateAsync
        // happened" (a swap to Revoke() would otherwise still pass this test).
        result.IsError.Should().BeFalse();
        await _requestRepository.Received(1).UpdateAsync(
            Arg.Is<PatientDoctorRequest>(r => r.Status == RequestStatus.Unlinked));
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenUpdateAsyncFails_ReturnsNotAcceptedWithoutTouchingPatient()
    {
        // Arrange — the request transitions in memory (Unlink() succeeds) but
        // persistence loses a concurrency race, so UpdateAsync returns false.
        // The patient must not be touched: only a persisted transition should
        // trigger the doctor detach.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _requestRepository.UpdateAsync(Arg.Any<PatientDoctorRequest>()).Returns(false);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(LinkRequestErrors.NotAccepted.Code);
        await _patientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(
            new UnlinkPatientCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }
}
