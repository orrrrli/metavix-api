using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Queries;

public sealed record GetLinkedDoctorsQuery(
    Guid PatientId) : IQuery<ErrorOr<List<LinkedDoctorResult>>>;
