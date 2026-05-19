using Application.UseCases.LinkRequest.Common;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record RevokeDoctorAccessCommand(
    Guid RequestId) : IRequest<ErrorOr<LinkRequestResult>>;
