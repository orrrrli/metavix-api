using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace API.Extensions;

public static class ErrorExtension
{
    public static IResult ToProblemResult(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return TypedResults.Problem(
                detail: "Se produjo un error no especificado",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error");
        }

        Error error = errors[0];

        if (error.NumericType == 0)
        {
            return TypedResults.Problem(
                detail: "Se produjo un error no especificado",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error");
        }

        var statusCode = GetStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Description,
        };

        // Agregar metadata si existe
        if (error.Metadata != null && error.Metadata.Any())
        {
            var metadata = new Dictionary<string, object>();
            foreach (var metadataItem in error.Metadata)
            {
                metadata.Add(metadataItem.Key, metadataItem.Value);
            }

            // Objeto llamado "metadata" que contiene toda la informacion adicional del error
            problemDetails.Extensions.Add("metadata", metadata);
        }

        return TypedResults.Problem(problemDetails);
    }

    private static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
}
