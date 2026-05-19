using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetPendingRequestsQuery(
    Guid DoctorId) : IRequest<ErrorOr<List<PendingRequestResult>>>;
