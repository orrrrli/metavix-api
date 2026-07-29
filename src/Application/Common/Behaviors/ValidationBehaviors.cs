using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;

namespace Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    private static readonly ConcurrentDictionary<Type, Func<List<Error>, TResponse>> Converters = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next();
        }

        ValidationResult? validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid)
        {
            return await next();
        }

        List<Error> errors = validationResult.Errors.ConvertAll(validationFailure =>
            Error.Validation(description: validationFailure.ErrorMessage));

        Func<List<Error>, TResponse> convert = Converters.GetOrAdd(typeof(TResponse), BuildConverter);
        return convert(errors);
    }

    /// <summary>
    /// TResponse is always ErrorOr&lt;TValue&gt; at the call sites MediatR builds, but the
    /// TValue isn't visible in this behavior's own generic parameters (MediatR's open-generic
    /// registration only supplies TRequest/TResponse). This extracts TValue from the closed
    /// TResponse at runtime and compiles a typed delegate to ErrorOrFactory.From&lt;TValue&gt;,
    /// cached per TResponse so the reflection cost is paid once, not per request.
    /// </summary>
    private static Func<List<Error>, TResponse> BuildConverter(Type responseType)
    {
        Type valueType = responseType.GetGenericArguments()[0];

        MethodInfo factoryMethod = typeof(ErrorOrFactory)
            .GetMethod(nameof(ErrorOrFactory.From), 1, [typeof(List<Error>)])!
            .MakeGenericMethod(valueType);

        ParameterExpression errorsParam = Expression.Parameter(typeof(List<Error>), "errors");
        MethodCallExpression call = Expression.Call(factoryMethod, errorsParam);

        return Expression.Lambda<Func<List<Error>, TResponse>>(call, errorsParam).Compile();
    }
}
