using Application.UseCases.Doctor.Commands;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Handlers;

namespace Application.Tests.Doctors;

public class UpdateDoctorProfileCommandHandlerTests
{
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly ICurrentUserService _currentUser    = Substitute.For<ICurrentUserService>();

    private readonly UpdateDoctorProfileCommandHandler _handler;

    public UpdateDoctorProfileCommandHandlerTests()
    {
        _handler = new UpdateDoctorProfileCommandHandler(_doctorRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenAuthorizedDoctorUpdatesCredentials_ReturnsUpdatedProfile()
    {
        // Arrange
        var userId   = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var command  = new UpdateDoctorProfileCommand("12345678", "Endocrinología");
        var doctor   = BuildDoctor(doctorId, userId, command.LicenseNumber, command.Speciality);

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(doctor);

        // Act
        ErrorOr<DoctorProfileResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.LicenseNumber.Should().Be("12345678");
        result.Value.Speciality.Should().Be("Endocrinología");
        await _doctorRepository.Received(1).UpdateProfileAsync(
            doctorId,
            command.LicenseNumber,
            command.Speciality,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoDoctorRecord_ReturnsDoctorNotFound()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var command = new UpdateDoctorProfileCommand("12345678", "Endocrinología");

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Doctor?)null);

        // Act
        ErrorOr<DoctorProfileResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(DoctorErrors.DoctorNotFound.Code);
        await _doctorRepository.DidNotReceive().UpdateProfileAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);
        var command = new UpdateDoctorProfileCommand("12345678", "Endocrinología");

        // Act
        ErrorOr<DoctorProfileResult> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.DidNotReceive()
            .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static Doctor BuildDoctor(Guid doctorId, Guid userId, string licenseNumber, string speciality) => new()
    {
        Id                = doctorId,
        UserId            = userId,
        FirstName         = "Ana",
        PaternalLastName  = "García",
        MaternalLastName  = "López",
        LicenseNumber     = licenseNumber,
        Speciality        = speciality,
        Email             = "ana@clinic.com",
        IsVerified        = false,
        IsActive          = true,
        CreatedAt         = DateTime.UtcNow,
    };
}
