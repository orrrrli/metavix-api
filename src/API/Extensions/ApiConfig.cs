namespace API.Extensions;

public static class ApiConfig
{
    public static WebApplication ConfigureApi(this WebApplication app)
    {
        // Configurar content type global
        app.Use(async (context, next) =>
        {
            context.Response.ContentType = "application/json";
            await next();
        });

        return app;
    }

    public static RouteGroupBuilder ConfigureApiGroup(this RouteGroupBuilder group) =>
        group.RequireAuthorization();
}
