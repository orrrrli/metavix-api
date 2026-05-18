using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.GlobalException;

/// <summary>
/// Manejador global de excepciones para la API.
/// Procesa todas las excepciones no manejadas y proporciona respuestas consistentes.
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Error procesando {RequestPath}. TraceId: {TraceId}",
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        (int statusCode, string? title, string? detail) = GetExceptionDetails(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        // En desarrollo, incluir detalles adicionales del error
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);
            problemDetails.Extensions.Add("timestamp", DateTime.UtcNow);
            problemDetails.Extensions.Add("exception", new
            {
                exception.Message,
                exception.StackTrace,
                InnerException = exception.InnerException?.Message
            });
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, jsonOptions, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) GetExceptionDetails(Exception exception) =>
        exception switch
        {
            TimeoutException timeoutEx => (
                StatusCodes.Status504GatewayTimeout,
                "Tiempo de espera agotado",
                "La operación no pudo completarse en el tiempo esperado. Por favor, inténtalo de nuevo más tarde."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado. Por favor, inténtalo de nuevo más tarde.")
        };
}
