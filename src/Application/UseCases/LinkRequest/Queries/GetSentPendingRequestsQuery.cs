using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetSentPendingRequestsQuery(
    Guid PatientId) : IRequest<ErrorOr<List<SentPendingRequestResult>>>;
