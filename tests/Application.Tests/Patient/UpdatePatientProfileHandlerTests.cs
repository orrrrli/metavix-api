using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Commands;
using Application.UseCases.Patient.Handlers;
using Domain.Enums;
using Domain.Models;

namespace Application.Tests.Patients;

public class UpdatePatientProfileHandlerTests
{
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IDoctorRepository _doctorRepository = Substitute.For<IDoctorRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly UpdatePatientProfileCommandHandler _handler;

    public UpdatePatientProfileHandlerTests()
    {
        _handler = new UpdatePatientProfileCommandHandler(
            _patientRepository,
            _doctorRepository,
            _notificationRepository,
            _currentUser);
    }

    // BE-FOUND-2-T3: male patient rejects IsPregnant=true
    [Fact]
    public async Task Handle_MalePatient_IsPregnantTrue_ReturnsPregnancyRequiresFemale()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Domain.Models.Patient
        {
            Id = patientId,
            UserId = userId,
            Gender = Gender.Male,
            IsPregnant = false,
            FirstName = "Juan",
            LastName = "Pérez"
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        var command = new UpdatePatientProfileCommand(patientId, IsPregnant: true, null, null, null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PatientErrors.PregnancyRequiresFemale.Code);
    }

    // BE-FOUND-2-T4: female patient accepts IsPregnant=true
    [Fact]
    public async Task Handle_FemalePatient_IsPregnantTrue_Succeeds()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patient = new Domain.Models.Patient
        {
            Id = patientId,
            UserId = userId,
            Gender = Gender.Female,
            IsPregnant = false,
            FirstName = "Ana",
            LastName = "García"
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        var command = new UpdatePatientProfileCommand(patientId, IsPregnant: true, null, null, null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.IsPregnant.Should().BeTrue();
    }

    // BE-FOUND-3: pregnancy activation creates notification for doctor
    [Fact]
    public async Task Handle_PregnancyTransition_WithDoctor_StagesNotification()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var doctorUserId = Guid.NewGuid();

        var patient = new Domain.Models.Patient
        {
            Id = patientId,
            UserId = userId,
            Gender = Gender.Female,
            IsPregnant = false,
            PrimaryDoctorId = doctorId,
            FirstName = "María",
            LastName = "López"
        };

        var doctor = new Doctor { Id = doctorId, UserId = doctorUserId };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);
        _doctorRepository.GetByIdAsync(doctorId).Returns(doctor);

        var command = new UpdatePatientProfileCommand(patientId, IsPregnant: true, null, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        _notificationRepository.Received(1).Stage(Arg.Is<Notification>(n =>
            n.RecipientUserId == doctorUserId &&
            n.PatientId == patientId &&
            n.Type == NotificationType.PregnancyActivated));
    }

    // BE-FOUND-3: no doctor → notification staged with null recipient (queued)
    [Fact]
    public async Task Handle_PregnancyTransition_NoPrimaryDoctor_StagesQueuedNotification()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var patient = new Domain.Models.Patient
        {
            Id = patientId,
            UserId = userId,
            Gender = Gender.Female,
            IsPregnant = false,
            PrimaryDoctorId = null,
            FirstName = "Laura",
            LastName = "Torres"
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        var command = new UpdatePatientProfileCommand(patientId, IsPregnant: true, null, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        _notificationRepository.Received(1).Stage(Arg.Is<Notification>(n =>
            n.RecipientUserId == null &&
            n.PatientId == patientId &&
            n.Type == NotificationType.PregnancyActivated));
    }

    // No transition (already pregnant) → no notification
    [Fact]
    public async Task Handle_AlreadyPregnant_DoesNotStageNotification()
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var patient = new Domain.Models.Patient
        {
            Id = patientId,
            UserId = userId,
            Gender = Gender.Female,
            IsPregnant = true,
            FirstName = "Rosa",
            LastName = "Díaz"
        };

        _currentUser.UserId.Returns(userId);
        _patientRepository.GetPatientIdByUserIdAsync(userId).Returns(patientId);
        _patientRepository.GetByIdAsync(patientId).Returns(patient);

        var command = new UpdatePatientProfileCommand(patientId, IsPregnant: true, null, null, null, null);
        await _handler.Handle(command, CancellationToken.None);

        _notificationRepository.DidNotReceive().Stage(Arg.Any<Notification>());
    }
}
