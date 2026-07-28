namespace Application.Common.Messaging;

public interface ICommand<TResponse> : IRequest<TResponse>;
