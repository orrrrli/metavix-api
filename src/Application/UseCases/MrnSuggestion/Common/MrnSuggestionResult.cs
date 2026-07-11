namespace Application.UseCases.MrnSuggestion.Common;

/// <summary>
/// Server-side MRN suggestion. The MRN belongs to the doctor-patient
/// RELATION, not the patient — when a link is unlinked the patient's
/// MedicalRecordNumber is cleared, freeing the value for re-use.
/// </summary>
public sealed record MrnSuggestionResult(string Mrn);
