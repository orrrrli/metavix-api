using Application.UseCases.MrnSuggestion.Common;

namespace Application.UseCases.MrnSuggestion.Queries;

/// <summary>
/// Asks the backend for the next available MRN suggestion for a year.
/// The caller (typically the doctor accepting a link request) gets a
/// pre-filled value they can override; uniqueness is enforced at the
/// DB index when the value is finally accepted.
/// </summary>
public sealed record GetMrnSuggestionQuery(
    int Year) : IRequest<ErrorOr<MrnSuggestionResult>>;
