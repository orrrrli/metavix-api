using Application.Common.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Postgres-backed implementation of <see cref="IMrnCounterRepository"/>.
/// Reads every non-empty MRN matching the year prefix and parses the
/// trailing 6-digit suffix in C# — the table is small enough (one row per
/// patient) that a simple in-memory max is cheaper than a regex or
/// substring index in Postgres.
/// </summary>
public class MrnCounterRepository : IMrnCounterRepository
{
    private const int SuffixLength = 6;
    private readonly AppDbContext _dbContext;

    public MrnCounterRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<int> GetMaxSequenceForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        string prefix = $"MRN-{year:D4}-";

        var suffixes = await _dbContext.Patients
            .Where(p => p.MedicalRecordNumber != null
                     && p.MedicalRecordNumber.StartsWith(prefix))
            .Select(p => p.MedicalRecordNumber!)
            .ToListAsync(cancellationToken);

        int max = 0;
        foreach (var mrn in suffixes)
        {
            // mrn has already been filtered by StartsWith(prefix), so the
            // suffix starts at position prefix.Length and is SuffixLength
            // chars wide. Defensive parse in case of legacy data.
            if (mrn.Length < prefix.Length + SuffixLength) continue;
            var span = mrn.AsSpan(prefix.Length, SuffixLength);
            if (!int.TryParse(span, out var n)) continue;
            if (n > max) max = n;
        }

        return max;
    }
}
