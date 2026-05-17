using ErrorOr;

namespace API.Helpers;

public static class ApiResponseHelper
{
    public static IResult HandleError(List<Error> errors)
    {
        return errors.Count == 0
            ? Results.Problem("Ocurrió un error desconocido.", statusCode: StatusCodes.Status500InternalServerError)
            : HandleError(errors[0]);
    }

    private static IResult HandleError(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: error.Type.ToString());
    }
}
