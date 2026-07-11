namespace Contracts.LinkRequest.Request;

/// <summary>
/// Body for POST /api/v1/doctor/requests/{id}/accept.
///
/// `MedicalRecordNumber` is optional — when omitted, the backend assigns
/// the next available MRN for the current year. When provided, the
/// backend validates the format and uniqueness.
/// </summary>
public record AcceptLinkRequestRequest(string? MedicalRecordNumber);
