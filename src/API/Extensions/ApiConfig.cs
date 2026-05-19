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
            await next();
        });

        return app;
    }

    public static RouteGroupBuilder ConfigureApiGroup(this RouteGroupBuilder group) =>
        group.RequireAuthorization();
}
