using Scalar.AspNetCore;

namespace API.Extensions;

public static class OpenApiConfig
{
    public static IServiceCollection AddOpenApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Info.Title = "Metavix API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseOpenApiDocs(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Metavix API";
            options.Theme = ScalarTheme.Moon;
        });

        return app;
    }
}
