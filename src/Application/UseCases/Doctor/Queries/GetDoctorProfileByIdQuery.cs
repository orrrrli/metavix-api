using Application.UseCases.Doctor.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetDoctorProfileByIdQuery(Guid DoctorId) : IQuery<ErrorOr<DoctorProfileResult>>;
