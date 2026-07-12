using Application.Common.Generators;
using Application.UseCases.MrnSuggestion.Common;
using Application.UseCases.MrnSuggestion.Queries;

namespace Application.UseCases.MrnSuggestion.Handlers;

internal sealed class GetMrnSuggestionQueryHandler
    : IRequestHandler<GetMrnSuggestionQuery, ErrorOr<MrnSuggestionResult>>
{
    private readonly TimeProvider _timeProvider;

    public GetMrnSuggestionQueryHandler(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<ErrorOr<MrnSuggestionResult>> Handle(
        GetMrnSuggestionQuery request,
        CancellationToken cancellationToken)
    {
        // The suggestion is always derived from "now" — there is no
        // per-year counter to clamp against anymore, so request.Year is
        // no longer used for the value itself (kept on the query for
        // backward API compatibility with existing callers).
        var suggestion = MrnGenerator.Suggest(_timeProvider.GetUtcNow());
        return Task.FromResult<ErrorOr<MrnSuggestionResult>>(new MrnSuggestionResult(suggestion));
    }
}
