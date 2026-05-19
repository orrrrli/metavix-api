using Application.UseCases.Doctor.Common;

namespace Application.UseCases.Doctor.Queries;

public sealed record GetAllDoctorsQuery() : IRequest<ErrorOr<List<DoctorResult>>>;
