using Application.UseCases.Doctor.Common;
using Application.Common.Messaging;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetMyDoctorProfileQuery : IQuery<ErrorOr<DoctorProfileResult>>;
