using Application.Common.Errors;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.UseCases.Patient.Commands;
using Application.UseCases.Patient.Common;
using Domain.Enums;
using Domain.Models;

namespace Application.UseCases.Patient.Handlers;

internal sealed class UpdatePatientProfileCommandHandler
    : IRequestHandler<UpdatePatientProfileCommand, ErrorOr<PatientProfileResult>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdatePatientProfileCommandHandler(
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<PatientProfileResult>> Handle(
        UpdatePatientProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return AuthErrors.Forbidden;

        var callerPatientId = await _patientRepository.GetPatientIdByUserIdAsync(_currentUser.UserId.Value);
        if (callerPatientId != request.PatientId)
            return AuthErrors.Forbidden;

        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient is null)
            return PatientErrors.PatientNotFound;

        if (request.IsPregnant == true && patient.Gender != Gender.Female)
            return PatientErrors.PregnancyRequiresFemale;

        bool pregnancyActivated = request.IsPregnant == true && !patient.IsPregnant;

        if (request.IsPregnant.HasValue)
            patient.IsPregnant = request.IsPregnant.Value;

        if (request.HeightCm.HasValue)
            patient.HeightCm = request.HeightCm.Value;

        if (request.Phone is not null)
            patient.Phone = request.Phone;

        if (request.PregnancyStartDate.HasValue)
            patient.PregnancyStartDate = request.PregnancyStartDate.Value;

        if (request.PregnancyDueDate.HasValue)
            patient.PregnancyDueDate = request.PregnancyDueDate.Value;

        if (pregnancyActivated)
        {
            Guid? recipientUserId = null;

            if (patient.PrimaryDoctorId.HasValue)
            {
                var doctor = await _doctorRepository.GetByIdAsync(patient.PrimaryDoctorId.Value);
                recipientUserId = doctor?.UserId;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            _notificationRepository.Stage(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientUserId,
                PatientId = patient.Id,
                Title = "Paciente ahora en embarazo",
                Body = $"{patient.FirstName} {patient.LastName} fue marcada como embarazada el {today}. Las metas clínicas de embarazo están activas.",
                Type = NotificationType.PregnancyActivated,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _patientRepository.UpdateAsync(patient);

        return new PatientProfileResult(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.Email,
            patient.Phone,
            patient.DateOfBirth,
            patient.HeightCm,
            patient.Gender?.ToString(),
            patient.IsPregnant,
            patient.DiabetesType.ToString(),
            patient.MedicalRecordNumber,
            patient.CreatedAt);
    }
}
