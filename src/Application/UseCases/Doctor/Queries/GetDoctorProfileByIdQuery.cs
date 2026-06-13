using Application.UseCases.Doctor.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetDoctorProfileByIdQuery(Guid DoctorId) : IRequest<ErrorOr<DoctorProfileResult>>;
