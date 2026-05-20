using API;
using API.Extensions;
using Application;
using Application.Common.Interfaces.Services;
using Carter;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, config) =>
    {
        string connectionString = context.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        IDictionary<string, ColumnWriterBase> columns = new Dictionary<string, ColumnWriterBase>
        {
            { "message",          new RenderedMessageColumnWriter() },
            { "message_template", new MessageTemplateColumnWriter() },
            { "level",            new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            { "raise_date",       new TimestampColumnWriter() },
            { "exception",        new ExceptionColumnWriter() },
            { "properties",       new LogEventSerializedColumnWriter() },
        };

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
                columnOptions: columns,
                needAutoCreateTable: true,
                batchSizeLimit: 50,
                period: TimeSpan.FromSeconds(5));
    });

    builder.Services
        .AddPresentation(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope())
    {
        IDatabaseValidator databaseValidator = scope.ServiceProvider.GetRequiredService<IDatabaseValidator>();
        await databaseValidator.ValidateAsync();
    }

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, _) =>
            httpContext.Request.Path.StartsWithSegments("/api/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
    });

    app.ConfigureApi();
    app.UseCors("ProductionPolicy");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOutputCache();
    app.UseOpenApiDocs();

    app.MapHealthChecks("/api/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
        }
    }).AllowAnonymous();

    RouteGroupBuilder apiGroup = app.MapGroup("/api");
    apiGroup.MapCarter();
    apiGroup.RequireAuthorization();

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
