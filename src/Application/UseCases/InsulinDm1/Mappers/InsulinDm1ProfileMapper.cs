using Application.UseCases.InsulinDm1.Common;
using Domain.Models;

namespace Application.UseCases.InsulinDm1.Mappers;

internal static class InsulinDm1ProfileMapper
{
    public static InsulinDm1ProfileResult ToResult(InsulinDm1Profile profile) => new(
        profile.Id,
        profile.PatientId,
        profile.InsulinName,
        profile.Ric,
        profile.SensitivityFactor,
        profile.TargetGlucose,
        profile.DoctorName,
        profile.DoctorPhone,
        profile.CreatedAt,
        profile.UpdatedAt);
}
