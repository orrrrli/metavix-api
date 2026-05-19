using API;
using API.Extensions;
using Application;
using Application.Common.Interfaces.Services;
using Carter;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

try
{

    var builder = WebApplication.CreateBuilder(args);

    builder.Services
        .AddPresentation(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Validate database connection
    using (IServiceScope scope = app.Services.CreateScope())
    {
        IDatabaseValidator databaseValidator = scope.ServiceProvider.GetRequiredService<IDatabaseValidator>();
        await databaseValidator.ValidateAsync(); // Ensure DB connection
    }

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();
    app.MapHealthChecks("/api/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var result = new
            {
                status = report.Status.ToString(),
                details = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                    })
            };

            await context.Response.WriteAsJsonAsync(result);
        }
    }).AllowAnonymous();
    
    app.ConfigureApi();
    app.UseOpenApiDocs();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    RouteGroupBuilder apiGroup = app.MapGroup("/api");
    apiGroup.MapCarter();
    apiGroup.RequireAuthorization();
    await app.RunAsync();
}
catch (Exception ex)
{
// Log any startup exceptions
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    Log.Fatal(ex, "Application startup failed due to an unexpected error.");
}
finally
{
    // Ensure the logger is flushed and disposed properly
    Log.CloseAndFlush();
}