using System.Security.Claims;
using API.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace API.Extensions;

internal static class LoggingExtensions
{
    internal static IHostBuilder AddSerilogLogging(this IHostBuilder host) =>
        host.UseSerilog((context, _, config) =>
        {
            string connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            config
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.PostgreSQL(
                    connectionString: connectionString,
                    tableName: "Logs",
                    columnOptions: LogTableColumns.Default,
                    needAutoCreateTable: true,
                    batchSizeLimit: 50,
                    period: TimeSpan.FromSeconds(5));
        });

    internal static WebApplication UseEnrichedRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, _) =>
                httpContext.Request.Path.StartsWithSegments("/api/health")
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                string? role   = httpContext.User.FindFirstValue(ClaimTypes.Role);

                if (userId is not null) diagnosticContext.Set("UserId", userId);
                if (role   is not null) diagnosticContext.Set("Role",   role);
            };
        });

        return app;
    }
}
