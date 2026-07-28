using Application.UseCases.Patient.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Patient.Commands;

public sealed record UpdatePatientProfileCommand(
    Guid PatientId,
    bool? IsPregnant,
    decimal? HeightCm,
    string? Phone,
    DateOnly? PregnancyStartDate,
    DateOnly? PregnancyDueDate) : ICommand<ErrorOr<PatientProfileResult>>;
