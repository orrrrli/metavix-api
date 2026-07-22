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
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>())
            .Returns(new Doctor { Id = doctorId });
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(true);
        _patientRepository.GetPatientByPatientId(patientId).Returns(patient);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(patient);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        // Arrange
        var (_, doctorId, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.DidNotReceive().GetOwnedDoctorAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRouteDoctorIdIsNotOwnedByCaller_ReturnsForbidden()
    {
        // Arrange — the route doctorId is not owned by the caller, so
        // DoctorPatientLinkAuth returns Forbidden before any link check.
        var (userId, _, patientId) = TestIds.DoctorLink();
        var otherDoctorId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(otherDoctorId, userId, Arg.Any<CancellationToken>())
            .Returns((Doctor?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(otherDoctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _requestRepository.DidNotReceive().IsAcceptedLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAcceptedLink_ReturnsForbidden()
    {
        // Arrange — route doctorId is owned by the caller, but no link
        // exists with this patient, so DoctorPatientLinkAuth returns Forbidden.
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>())
            .Returns(new Doctor { Id = doctorId });
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive().GetPatientByPatientId(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenLinkedButPatientRecordMissing_ReturnsPatientNotFound()
    {
        // Arrange — DoctorPatientLinkAuth has already closed the enumeration
        // oracle (the caller is the route doctor and holds an accepted link),
        // so reaching this branch means the link points at a patient record
        // that has since been deleted. That is an inconsistent state, not an
        // enumeration probe — surface PatientNotFound honestly, matching
        // GetLinkedPatientProfileQueryHandler.
        var (userId, doctorId, patientId) = TestIds.DoctorLink();
        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetOwnedDoctorAsync(doctorId, userId, Arg.Any<CancellationToken>())
            .Returns(new Doctor { Id = doctorId });
        _requestRepository.IsAcceptedLinkAsync(doctorId, patientId, Arg.Any<CancellationToken>()).Returns(true);
        _patientRepository.GetPatientByPatientId(patientId).Returns((PatientResult?)null);

        // Act
        var result = await _handler.Handle(new PatientByIdQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PatientNotFound.Code);
    }
}
