using Carter;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace API.Extensions;

public static class ApiConfig
{
    public static WebApplication ConfigureApi(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.ContentType = "application/json";
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            await next();
        });

        return app;
    }

    public static WebApplication MapHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
            }
        }).AllowAnonymous();

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        RouteGroupBuilder apiGroup = app.MapGroup("/api/v{version:apiVersion}");
        apiGroup.MapCarter();
        apiGroup.RequireAuthorization();

        return app;
    }
}
