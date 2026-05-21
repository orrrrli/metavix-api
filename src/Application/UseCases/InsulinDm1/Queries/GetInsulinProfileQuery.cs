using Application.UseCases.InsulinDm1.Common;

namespace Application.UseCases.InsulinDm1.Queries;

public sealed record GetInsulinProfileQuery(Guid PatientId) : IRequest<ErrorOr<InsulinDm1ProfileResult>>;
