namespace Application.Common.Generators;

/// <summary>
/// Suggests a Medical Record Number derived from the current timestamp.
///
/// The doctor is the authoritative source for the MRN — this helper only
/// produces a *suggested* value. The submitter may keep the suggestion or
/// type a custom value, as long as it matches <c>^MRN-\d{8}-\d{9}$</c> and
/// is unique in the database.
///
/// Timestamp-based (down to the millisecond) instead of a per-year sequence:
/// a sequence read from "max MRN currently in use" goes stale the moment a
/// patient is unlinked (its MRN is cleared to null, so the next auto-assign
/// recomputes the same max and reissues the same number). A wall-clock
/// value never repeats under normal use, so unlinking a patient can't cause
/// a collision. The DB unique index on <c>Patients.MedicalRecordNumber</c>
/// remains the actual enforcement for the rare same-millisecond race.
/// </summary>
public static class MrnGenerator
{
    /// <summary>
    /// Builds an MRN suggestion in the form <c>MRN-yyyyMMdd-HHmmssfff</c>.
    /// </summary>
    /// <param name="now">Timestamp the MRN is derived from.</param>
    public static string Suggest(DateTimeOffset now) =>
        $"MRN-{now:yyyyMMdd}-{now:HHmmssfff}";
}
