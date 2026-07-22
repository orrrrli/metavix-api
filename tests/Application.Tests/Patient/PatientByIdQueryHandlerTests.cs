using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Handlers;
using Application.UseCases.Patient.Queries;

namespace Application.Tests.Patients;

public class PatientByIdQueryHandlerTests
{
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly IPatientDoctorRequestRepository _requestRepository =
        Substitute.For<IPatientDoctorRequestRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly PatientByIdQueryHandler _handler;

    public PatientByIdQueryHandlerTests()
    {
        _handler = new PatientByIdQueryHandler(
            _patientRepository, _doctorRepository, _requestRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenLinkedAndPatientExists_ReturnsPatient()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        var patient = new PatientResult(patientId, "Jane", "Doe", "MRN-1");

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(true);
        _patientRepository.GetPatientByPatientId(patientId).Returns(patient);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(patient);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.DidNotReceive().GetDoctorIdByUserIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoDoctorProfile_ReturnsForbidden()
    {
        // Arrange
        var (userId, _, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().IsAcceptedLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive().GetPatientByPatientId(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenLinkedButPatientRecordMissing_ReturnsForbiddenNotNotFound()
    {
        // Arrange — an accepted link whose patient record has since disappeared
        // must be indistinguishable from "not linked": both return Forbidden.
        // Returning PatientNotFound here would let an authenticated doctor probe
        // arbitrary patientIds and learn which ones exist in the system.
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(true);
        _patientRepository.GetPatientByPatientId(patientId).Returns((PatientResult?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
    }
}
