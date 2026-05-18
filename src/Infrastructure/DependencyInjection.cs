using Application.Common.Interfaces.Services;
using Infrastructure.HealthChecks;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddServices()
            //.AddAuth(configuration)
            .AddPersistence(configuration);
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IDatabaseValidator, DatabaseValidator>();
        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContextFactory<AppDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(
                        5,
                        TimeSpan.FromSeconds(10),
                        null)
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));

            opts.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                opts.EnableSensitiveDataLogging();
                opts.EnableDetailedErrors();
            }
        });

        services.AddRepositories();
        services
          .AddHealthChecks()
          .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    /* private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    
    {
        JwtSettings jwtSettings = new();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);

        services.AddSingleton(Options.Create(jwtSettings));

        services.AddSingleton<ICriptography, Criptography>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            });
        services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);

        return services;
    }
    */
    
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        IEnumerable<Type> repositoryTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repository") && !t.IsAbstract && t.IsClass);
        foreach (Type? type in repositoryTypes)
        {
            Type? interfaceType = type.GetInterface($"I{type.Name}");
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, type);
            }
        }

        return services;
    }
}
