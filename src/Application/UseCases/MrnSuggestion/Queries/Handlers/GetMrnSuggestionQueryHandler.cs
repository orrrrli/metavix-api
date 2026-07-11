using Application.Common.Generators;
using Application.Common.Interfaces.Persistence;
using Application.UseCases.MrnSuggestion.Common;
using Application.UseCases.MrnSuggestion.Queries;

namespace Application.UseCases.MrnSuggestion.Handlers;

internal sealed class GetMrnSuggestionQueryHandler
    : IRequestHandler<GetMrnSuggestionQuery, ErrorOr<MrnSuggestionResult>>
{
    private readonly IMrnCounterRepository _counterRepository;
    private readonly TimeProvider _timeProvider;

    public GetMrnSuggestionQueryHandler(
        IMrnCounterRepository counterRepository,
        TimeProvider timeProvider)
    {
        _counterRepository = counterRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<MrnSuggestionResult>> Handle(
        GetMrnSuggestionQuery request,
        CancellationToken cancellationToken)
    {
        // Clamp year to current year — we never issue MRNs for past or
        // future years (the per-year counter is global).
        var now = _timeProvider.GetUtcNow();
        int year = request.Year == 0 ? now.Year : request.Year;
        if (year != now.Year) year = now.Year;

        var maxForYear = await _counterRepository.GetMaxSequenceForYearAsync(year, cancellationToken);
        var suggestion = MrnGenerator.Suggest(maxForYear, new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero));

        return new MrnSuggestionResult(suggestion);
    }
}
