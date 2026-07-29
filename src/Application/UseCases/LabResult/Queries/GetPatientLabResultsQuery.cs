using Application.UseCases.LabResult.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LabResult.Queries;

public sealed record GetPatientLabResultsQuery(
    Guid PatientId) : IQuery<ErrorOr<List<LabResultResult>>>;
