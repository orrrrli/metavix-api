using API;
using API.Extensions;
using API.Middleware;
using Application;
using Application.Common.Interfaces.Services;
using Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddSerilogLogging();

    builder.Services
        .AddPresentation(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<IDatabaseValidator>().ValidateAsync();

    app.UseResponseCompression();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseEnrichedRequestLogging();

    app.UseRouting();
    app.UseCors("ProductionPolicy");
    app.ConfigureApi();
    app.UseRateLimiter();
    app.UseRequestTimeouts();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOutputCache();
    app.UseOpenApiDocs();

    app.MapHealthCheck();
    app.MapApiEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed due to an unexpected error.");
}
finally
{
    Log.CloseAndFlush();
}
