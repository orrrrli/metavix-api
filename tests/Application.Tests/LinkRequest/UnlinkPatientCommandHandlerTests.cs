using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Handlers;

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
        _doctorRepository.GetDoctorIdByUserIdAsync(userId).Returns(doctorId);
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
}
