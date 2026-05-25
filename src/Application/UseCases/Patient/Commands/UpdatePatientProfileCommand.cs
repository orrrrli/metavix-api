using Application.UseCases.Patient.Common;

namespace Application.UseCases.Patient.Commands;

public sealed record UpdatePatientProfileCommand(
    Guid PatientId,
    bool? IsPregnant,
    decimal? HeightCm,
    string? Phone) : IRequest<ErrorOr<PatientProfileResult>>;
