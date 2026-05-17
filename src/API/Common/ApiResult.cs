using API.Extensions;
using API.Helpers;
using Contracts.Common;
using ErrorOr;

namespace API.Common;

public record ApiSuccessResponse<T>
{
    public required T Data { get; init; }
}

public static class ApiResults
{
    public static IResult Success<T>(T data, string route, object? logData = null, bool isPaginated = false)
    {
        return data is null
            ? TypedResults.Ok()
            : isPaginated ? TypedResults.Ok(data) : TypedResults.Ok(new ApiSuccessResponse<T> { Data = data });
    }

    public static IResult Created<T>(T action, int id, string route, string message = "Registro creado exitosamente")
    {
        CreatedResponse data = new(id, message);
        return TypedResults.Created(
            nameof(action),
            data);
    }
    
    public static IResult Problem(List<Error> errors, string route)
    {
        LoggingHelper.LogWarning(route, errors);
        return errors.ToProblemResult();
    }

    public static IResult Error(Exception ex, string route, object request)
    {
        LoggingHelper.LogError(route, ex, request);
        return TypedResults.Problem(
            detail: ex.InnerException?.Message ?? ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Ocurrió un error inesperado. Por favor, inténtalo de nuevo más tarde.");
    }
}
