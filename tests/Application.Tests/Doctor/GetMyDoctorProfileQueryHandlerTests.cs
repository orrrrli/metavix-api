using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Handlers;
using Application.UseCases.Doctor.Queries;

namespace Application.Tests.Doctors;

public class GetMyDoctorProfileQueryHandlerTests
{
    private readonly IDoctorRepository _doctorRepository =
        Substitute.For<IDoctorRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly GetMyDoctorProfileQueryHandler _handler;

    public GetMyDoctorProfileQueryHandlerTests()
    {
        _handler = new GetMyDoctorProfileQueryHandler(_doctorRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCallerHasDoctor_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var doctor = new Doctor
        {
            Id = doctorId,
            UserId = userId,
            FirstName = "Ana",
            PaternalLastName = "García",
            LicenseNumber = "12345678",
            Speciality = "Endocrinología",
            IsVerified = true,
            IsActive = true,
        };

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(doctor);

        var query = new GetMyDoctorProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(doctorId);
        result.Value.LicenseNumber.Should().Be("12345678");
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoDoctorRecord_ReturnsDoctorNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _doctorRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Doctor?)null);

        var query = new GetMyDoctorProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(DoctorErrors.DoctorNotFound.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        var query = new GetMyDoctorProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _doctorRepository.DidNotReceive()
            .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
