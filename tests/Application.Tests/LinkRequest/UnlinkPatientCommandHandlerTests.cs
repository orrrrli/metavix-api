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

        var linkRequest = TestEntities.LinkRequest(requestId, patientId, doctorId, RequestStatus.Accepted);
        var patient = new Patient
        {
            Id = patientId,
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan@mail.com",
            PrimaryDoctorId = doctorId,
            MedicalRecordNumber = mrn,
        };
        var doctor = TestEntities.Doctor(doctorId, userId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>()).Returns(doctor);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        // Act
        var result = await _handler.Handle(new UnlinkPatientCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
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
        result.FirstError.Code.Should().Be("LinkRequest.NotAccepted");
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
        result.FirstError.Code.Should().Be("Auth.Forbidden");
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
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

}
