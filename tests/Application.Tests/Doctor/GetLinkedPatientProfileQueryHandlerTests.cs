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
            new GetLinkedPatientProfileQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(patientId);
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
            new GetLinkedPatientProfileQuery(doctorId, patientId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PatientNotFound.Code);
    }
}
