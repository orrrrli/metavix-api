using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Handlers;

namespace Application.Tests.InsulinDm1;

public class DeleteInsulinRecordCommandHandlerTests
{
    private readonly IInsulinDm1Repository _insulinRepository =
        Substitute.For<IInsulinDm1Repository>();
    private readonly IPatientRepository _patientRepository =
        Substitute.For<IPatientRepository>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();

    private readonly DeleteInsulinRecordCommandHandler _handler;

    public DeleteInsulinRecordCommandHandlerTests()
    {
        _handler = new DeleteInsulinRecordCommandHandler(
            _insulinRepository,
            _patientRepository,
            _currentUser);
    }

    [Fact]
    public async Task Handle_WhenPatientIsOwnedAndRecordBelongsToPatient_DeletesRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var patient = TestEntities.Patient(patientId);
        var record = new InsulinDm1Record { Id = recordId, PatientId = patientId };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _insulinRepository.GetRecordByIdAsync(recordId).Returns(record);

        var command = new DeleteInsulinRecordCommand(patientId, recordId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _insulinRepository.Received(1).DeleteRecordAsync(record);
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotOwned_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetOwnedPatientAsync(patientId, userId, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var command = new DeleteInsulinRecordCommand(patientId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _insulinRepository.DidNotReceive().GetRecordByIdAsync(Arg.Any<Guid>());
        await _insulinRepository.DidNotReceive().DeleteRecordAsync(Arg.Any<InsulinDm1Record>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new DeleteInsulinRecordCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(AuthErrors.Forbidden.Code);
        await _patientRepository.DidNotReceive()
            .GetOwnedPatientAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

}
