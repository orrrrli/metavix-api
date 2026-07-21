using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Handlers;

namespace Application.Tests.LinkRequest;

public class AcceptLinkRequestCommandHandlerTests
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

    private readonly AcceptLinkRequestCommandHandler _handler;

    public AcceptLinkRequestCommandHandlerTests()
    {
        _handler = new AcceptLinkRequestCommandHandler(
            _requestRepository,
            _patientRepository,
            _doctorRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenDoctorHasLicenseNumberAndIsNotVerified_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000001";
        var now = DateTime.UtcNow;

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: false);
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _doctorRepository.GetByIdAsync(doctorId).Returns(doctor);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(false);
        _timeProvider.SetUtcNow(now);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RequestId.Should().Be(requestId);
        result.Value.Status.Should().Be("Accepted");
        await _patientRepository.Received(1).UpdateAsync(Arg.Is<Patient>(p =>
            p.MedicalRecordNumber == mrn));
        await _requestRepository.Received(1).UpdateAsync(Arg.Is<PatientDoctorRequest>(r =>
            r.Status == RequestStatus.Accepted));
    }

    [Fact]
    public async Task Handle_WhenMrnAlreadyAssigned_ReturnsConflict()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000042";

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.MrnAlreadyAssigned");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenPendingRequest_AssignsMrnToPatient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000123";
        var now = DateTime.UtcNow;

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(false);
        _timeProvider.SetUtcNow(now);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _patientRepository.Received(1).UpdateAsync(Arg.Is<Patient>(p =>
            p.MedicalRecordNumber == mrn && p.PrimaryDoctorId == doctorId));
    }

    [Fact]
    public async Task Handle_WhenPatientDeletedBeforeAccept_ReturnsPatientNotFoundWithoutMutating()
    {
        // Arrange — request + doctor resolve fine, but the patient was deleted
        // between sending and accepting. The handler must bail out before
        // accepting the request, so neither the request nor the patient is
        // mutated and no inconsistent Accepted-but-unlinked state is left.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000123";

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns((Patient?)null);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PatientNotFound.Code);
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
        linkRequest.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenMrnNotProvided_AutoAssignsNextAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _patientRepository.ExistsByMedicalRecordNumberAsync("MRN-20260711-120000000", Arg.Any<CancellationToken>()).Returns(false);
        _timeProvider.SetUtcNow(now);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, MedicalRecordNumber: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _patientRepository.Received(1).UpdateAsync(Arg.Is<Patient>(p =>
            p.MedicalRecordNumber == "MRN-20260711-120000000" && p.PrimaryDoctorId == doctorId));
    }

    [Fact]
    public async Task Handle_WhenAutoAssignExhaustsRetries_ReturnsAutoAssignFailed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        // Every candidate collides — simulate a pathological same-millisecond race.
        _patientRepository.ExistsByMedicalRecordNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _timeProvider.SetUtcNow(now);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, MedicalRecordNumber: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.MrnAutoAssignFailed");
    }

    [Fact]
    public async Task Handle_WhenNotPending_ReturnsNotPendingWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        linkRequest.Status = RequestStatus.Rejected;
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);

        // Act
        var result = await _handler.Handle(new AcceptLinkRequestCommand(requestId, MedicalRecordNumber: null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotPending");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    private static PatientDoctorRequest BuildLinkRequest(Guid requestId, Guid patientId, Guid doctorId) => new()
    {
        Id = requestId,
        PatientId = patientId,
        DoctorId = doctorId,
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.UtcNow,
    };


}
