namespace Application.Common.Generators;

/// <summary>
/// Suggests the next Medical Record Number for a given year.
///
/// The doctor is the authoritative source for the MRN — this helper only
/// produces a *suggested* value. The submitter may keep the suggestion or
/// type a custom value, as long as it matches <c>^MRN-\d{4}-\d{6}$</c> and
/// is unique in the database.
///
/// Because the suggestion is just a hint (not a strict sequence), a small
/// race-condition window between "max read" and "insert" is acceptable:
/// the unique index on <c>Patients.MedicalRecordNumber</c> is the actual
/// enforcement. Cluster-safe enough for the doctor-accept flow.
/// </summary>
public static class MrnGenerator
{
    /// <summary>
    /// Builds an MRN suggestion in the form <c>MRN-YYYY-NNNNNN</c> by
    /// appending <c>1</c> to the highest sequence observed for the year.
    /// </summary>
    /// <param name="existingMaxForYear">
    /// Highest 6-digit suffix already in use for the given year, or <c>0</c>
    /// when no records exist yet.
    /// </param>
    /// <param name="now">Timestamp that pins the year component.</param>
    public static string Suggest(int existingMaxForYear, DateTimeOffset now) =>
        $"MRN-{now.Year:D4}-{(existingMaxForYear + 1):D6}";
}
