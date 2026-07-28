using Application.UseCases.LinkRequest.Common;
using Application.Common.Messaging;

namespace Application.UseCases.LinkRequest.Commands;

public sealed record RevokeDoctorAccessCommand(
    Guid RequestId) : ITransactionalCommand<ErrorOr<LinkRequestResult>>;
