using Application.UseCases.Doctor.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetDoctorProfileQuery(
    Guid DoctorId) : IRequest<ErrorOr<DoctorProfileResult>>;
