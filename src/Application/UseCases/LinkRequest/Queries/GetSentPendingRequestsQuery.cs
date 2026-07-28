using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetSentPendingRequestsQuery(
    Guid PatientId) : IQuery<ErrorOr<List<SentPendingRequestResult>>>;
