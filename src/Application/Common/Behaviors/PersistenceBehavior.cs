using Application.Common.Interfaces.Persistence;
using Application.Common.Messaging;

namespace Application.Common.Behaviors;

public class PersistenceBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsError)
            return response;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
