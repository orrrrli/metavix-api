using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Handlers;

namespace Application.Tests.LinkRequest;

public class RevokeDoctorAccessCommandHandlerTests
{
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly FakeTimeProvider _timeProvider = new();

    private readonly RevokeDoctorAccessCommandHandler _handler;

    public RevokeDoctorAccessCommandHandlerTests()
    {
        _handler = new RevokeDoctorAccessCommandHandler(
            _requestRepository,
            _patientRepository,
            _currentUser,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenAccepted_RemovesDoctorButKeepsMrn()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var mrn = "MRN-2026-000042";
        var now = DateTime.UtcNow;

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
        };
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan@mail.com",
            PrimaryDoctorId = doctorId,
            MedicalRecordNumber = mrn,
        };

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _timeProvider.SetUtcNow(now);

        // Act
        var result = await _handler.Handle(new RevokeDoctorAccessCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Revoked");
        await _requestRepository.Received(1).UpdateAsync(Arg.Is<PatientDoctorRequest>(r =>
            r.Status == RequestStatus.Revoked && r.ResolvedAt == now));
        await _patientRepository.Received(1).UpdateAsync(Arg.Is<Patient>(p =>
            p.PrimaryDoctorId == null && p.MedicalRecordNumber == mrn));
    }

    [Fact]
    public async Task Handle_WhenNotAccepted_ReturnsNotAcceptedWithoutMutating()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan@mail.com",
        };

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        // Act
        var result = await _handler.Handle(new RevokeDoctorAccessCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("LinkRequest.NotAccepted");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotThePatient_ReturnsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
        };

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        // Caller is not the owner of the patient on the request.
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        // Act
        var result = await _handler.Handle(new RevokeDoctorAccessCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.Forbidden");
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

    [Fact]
    public async Task Handle_WhenPatientDeletedAfterRequestLoaded_ReturnsForbiddenWithoutMutating()
    {
        // Arrange — the link request still exists, but the patient it points at
        // has been deleted before the revoke ran, so GetOwnedPatientAsync yields
        // null. The handler must bail out before touching either the request or
        // the (now missing) patient.
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
        };

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        // Act
        var result = await _handler.Handle(new RevokeDoctorAccessCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
        await _patientRepository.DidNotReceive().UpdateAsync(Arg.Any<Patient>());
    }

    [Fact]
    public async Task Handle_WhenPatientPrimaryDoctorAlreadyChanged_DoesNotOverwriteNewDoctor()
    {
        // Arrange: patient already re-linked to a different doctor by the time
        // this revoke lands (stale-write guard via DetachPrimaryDoctor).
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var newDoctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = new PatientDoctorRequest
        {
            Id = requestId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = RequestStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
        };
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan@mail.com",
            PrimaryDoctorId = newDoctorId,
        };

        _currentUser.UserId.Returns(userId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);

        // Act
        var result = await _handler.Handle(new RevokeDoctorAccessCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        patient.PrimaryDoctorId.Should().Be(newDoctorId);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(
            new RevokeDoctorAccessCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }
}
