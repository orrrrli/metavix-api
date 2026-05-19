using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetLinkedDoctorsQuery(
    Guid PatientId) : IRequest<ErrorOr<List<LinkedDoctorResult>>>;
