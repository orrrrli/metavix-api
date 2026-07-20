namespace Domain.Models;

using Enums;

public class PatientDoctorRequest
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;

    // State-machine transitions. Each returns false (without mutating) when the request is not in
    // the required source state, so the caller can surface the right error; true means the
    // transition happened and ResolvedAt was stamped. Centralizes the legal-transition rule and the
    // Status/ResolvedAt pairing that was previously duplicated across the link handlers.
    public bool Accept(DateTime nowUtc) => TransitionFrom(RequestStatus.Pending, RequestStatus.Accepted, nowUtc);

    public bool Reject(DateTime nowUtc) => TransitionFrom(RequestStatus.Pending, RequestStatus.Rejected, nowUtc);

    public bool Unlink(DateTime nowUtc) => TransitionFrom(RequestStatus.Accepted, RequestStatus.Unlinked, nowUtc);

    public bool Revoke(DateTime nowUtc) => TransitionFrom(RequestStatus.Accepted, RequestStatus.Revoked, nowUtc);

    private bool TransitionFrom(RequestStatus expected, RequestStatus next, DateTime nowUtc)
    {
        if (Status != expected)
            return false;

        // ResolvedAt maps to a timestamptz column — a local DateTime.Kind here would silently
        // represent local time as if it were UTC. Callers must pass TimeProvider.GetUtcNow().UtcDateTime.
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{nameof(nowUtc)} must be UTC (Kind={nowUtc.Kind}).", nameof(nowUtc));

        Status = next;
        ResolvedAt = nowUtc;
        return true;
    }
}
