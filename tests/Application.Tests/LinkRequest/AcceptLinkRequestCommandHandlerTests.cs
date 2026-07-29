using Application.Common.Exceptions;
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
    private readonly IUnitOfWork _unitOfWork =
        Substitute.For<IUnitOfWork>();

    private readonly AcceptLinkRequestCommandHandler _handler;

    public AcceptLinkRequestCommandHandlerTests()
    {
        _handler = new AcceptLinkRequestCommandHandler(
            _requestRepository,
            _patientRepository,
            _doctorRepository,
            _currentUser,
            _timeProvider,
            _unitOfWork);
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
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
        patient.MedicalRecordNumber.Should().Be(mrn);
        linkRequest.Status.Should().Be(RequestStatus.Accepted);
        _requestRepository.Received(1).MarkForUpdate(linkRequest);
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.MrnAlreadyAssigned");
        _requestRepository.DidNotReceive().MarkForUpdate(Arg.Any<PatientDoctorRequest>());
        patient.MedicalRecordNumber.Should().NotBe(mrn);
        linkRequest.Status.Should().Be(RequestStatus.Pending);
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
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
        patient.MedicalRecordNumber.Should().Be(mrn);
        patient.PrimaryDoctorId.Should().Be(doctorId);
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
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
        _requestRepository.DidNotReceive().MarkForUpdate(Arg.Any<PatientDoctorRequest>());
        await _unitOfWork.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
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
        patient.MedicalRecordNumber.Should().Be("MRN-20260711-120000000");
        patient.PrimaryDoctorId.Should().Be(doctorId);
    }

    [Fact]
    public async Task Handle_WhenAutoAssignedMrnAlreadyExists_ReturnsAutoAssignFailed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(TestEntities.Patient(patientId));
        // The single auto-assigned candidate already exists — the same-millisecond
        // race that the DB unique index guards against.
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
    public async Task Handle_WhenConcurrentAcceptWinsRace_ReturnsNotPendingWithoutLinkingPatient()
    {
        // Arrange — the in-memory state is Pending, but a concurrent acceptance
        // committed first, so the intermediate FlushAsync throws
        // ConcurrencyConflictException. The handler must bail out as
        // NotPending and never apply the patient link.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000123";

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
        var doctor = TestEntities.Doctor(doctorId, licenseNumber: "12345678", isVerified: true);
        var patient = TestEntities.Patient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _patientRepository.ExistsByMedicalRecordNumberAsync(mrn, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.FlushAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        // Act
        var result = await _handler.Handle(new AcceptLinkRequestCommand(requestId, mrn), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotPending");
        patient.MedicalRecordNumber.Should().NotBe(mrn);
    }

    [Fact]
    public async Task Handle_WhenNotPending_ReturnsNotPendingWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId);
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
        _requestRepository.DidNotReceive().MarkForUpdate(Arg.Any<PatientDoctorRequest>());
        await _unitOfWork.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }
}
