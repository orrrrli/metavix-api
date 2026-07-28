using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetPendingRequestsQuery(
    Guid DoctorId) : IQuery<ErrorOr<List<PendingRequestResult>>>;
