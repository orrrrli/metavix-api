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
    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    private readonly AcceptLinkRequestCommandHandler _handler;

    public AcceptLinkRequestCommandHandlerTests()
    {
        _handler = new AcceptLinkRequestCommandHandler(
            _requestRepository,
            _patientRepository,
            _doctorRepository,
            _currentUser,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenDoctorHasLicenseNumberAndIsNotVerified_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = BuildDoctor(doctorId, licenseNumber: "12345678", isVerified: false);
        var patient = BuildPatient(patientId);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _doctorRepository.GetByIdAsync(doctorId).Returns(doctor);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _dateTimeProvider.UtcNow.Returns(now);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RequestId.Should().Be(requestId);
        result.Value.Status.Should().Be("Accepted");
        await _requestRepository.Received(1).UpdateAsync(Arg.Is<PatientDoctorRequest>(r =>
            r.Status == RequestStatus.Accepted));
    }

    [Fact]
    public async Task Handle_WhenDoctorHasEmptyLicenseNumber_ReturnsNotVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var linkRequest = BuildLinkRequest(requestId, patientId, doctorId);
        var doctor = BuildDoctor(doctorId, licenseNumber: string.Empty, isVerified: false);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
        _requestRepository.GetByIdAsync(requestId).Returns(linkRequest);
        _doctorRepository.GetByIdAsync(doctorId).Returns(doctor);

        // Act
        ErrorOr<LinkRequestResult> result =
            await _handler.Handle(new AcceptLinkRequestCommand(requestId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(DoctorErrors.NotVerified.Code);
        await _requestRepository.DidNotReceive().UpdateAsync(Arg.Any<PatientDoctorRequest>());
    }

    private static PatientDoctorRequest BuildLinkRequest(Guid requestId, Guid patientId, Guid doctorId) => new()
    {
        Id = requestId,
        PatientId = patientId,
        DoctorId = doctorId,
        Status = RequestStatus.Pending,
        CreatedAt = DateTime.UtcNow,
    };

    private static Doctor BuildDoctor(Guid doctorId, string licenseNumber, bool isVerified) => new()
    {
        Id = doctorId,
        FirstName = "Ana",
        PaternalLastName = "García",
        MaternalLastName = "López",
        LicenseNumber = licenseNumber,
        Speciality = "Endocrinología",
        Email = "ana@clinic.com",
        UserId = Guid.NewGuid(),
        IsVerified = isVerified,
    };

    private static Patient BuildPatient(Guid patientId) => new()
    {
        Id = patientId,
        FirstName = "Juan",
        LastName = "Pérez",
        Email = "juan@mail.com",
    };
}
