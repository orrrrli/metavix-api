using System.IO.Compression;
using System.Text.Json.Serialization;
using API.Extensions;
using API.GlobalException;
using Carter;
using Contracts.Common;
using Microsoft.AspNetCore.ResponseCompression;


namespace API;

public static class DependecyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddCustomCors(configuration)
            .AddOpenApiDocs()
            .AddCarter()
            .AddGlobalExceptionHandling()
            .AddProblemDetails()
            .AddOutputCacheConfig()
            .AddRequestTimeoutConfig(configuration)
            .AddMapping()
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new Converters.DateOnlyJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new Converters.TimeOnlyJsonConverter());
            });

        services
            .AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
            })
            .Configure<BrotliCompressionProviderOptions>(options =>
                options.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(options =>
                options.Level = CompressionLevel.SmallestSize);

        return services;
    }

    private static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[]? allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

        services.AddCors(options => options.AddPolicy("ProductionPolicy", builder =>
            {
                if (allowedOrigins != null)
                {
                    builder.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            }));

        return services;
    }

    private static IServiceCollection AddOutputCacheConfig(this IServiceCollection services)
    {
        services.AddOutputCache(options => options.AddPolicy("CustomDefaultPolicy", CustomOutputCachePolicy.Instance));

        return services;
    }

    private static IServiceCollection AddRequestTimeoutConfig(this IServiceCollection services, IConfiguration configuration)
    {
        int defaultTimeoutSeconds = configuration.GetValue<int>("RequestTimeouts:DefaultTimeoutSeconds", 60);
        int longRunningTimeoutSeconds = configuration.GetValue<int>("RequestTimeouts:LongRunningTimeoutSeconds", 120);
        int quickLookupTimeoutSeconds = configuration.GetValue<int>("RequestTimeouts:QuickLookupTimeoutSeconds", 30);

        services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(defaultTimeoutSeconds)
            };

            options.AddPolicy("LongRunning", TimeSpan.FromSeconds(longRunningTimeoutSeconds));
            options.AddPolicy("QuickLookup", TimeSpan.FromSeconds(quickLookupTimeoutSeconds));
        });

        return services;
    }
}