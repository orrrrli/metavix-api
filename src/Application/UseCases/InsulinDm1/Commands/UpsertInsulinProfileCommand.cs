using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Commands;

public sealed record UpsertInsulinProfileCommand(
    Guid PatientId,
    string? InsulinName,
    decimal? Ric,
    int? SensitivityFactor,
    int? TargetGlucose,
    string? DoctorName,
    string? DoctorPhone) : IRequest<ErrorOr<InsulinDm1ProfileResult>>;
