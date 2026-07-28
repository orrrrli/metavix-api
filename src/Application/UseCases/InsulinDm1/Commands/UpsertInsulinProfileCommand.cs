using Application.UseCases.InsulinDm1.Common;
using Application.Common.Messaging;

namespace Application.UseCases.InsulinDm1.Commands;

public sealed record UpsertInsulinProfileCommand(
    Guid PatientId,
    string? InsulinName,
    decimal? Ric,
    int? SensitivityFactor,
    int? TargetGlucose,
    string? DoctorName,
    string? DoctorPhone) : ICommand<ErrorOr<InsulinDm1ProfileResult>>;
