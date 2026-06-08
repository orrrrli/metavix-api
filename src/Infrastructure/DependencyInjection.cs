using System.Text;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.Services;
using Infrastructure.HealthChecks;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddServices(configuration)
            .AddAuth(configuration)
            .AddPersistence(configuration);
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();
        services.AddScoped<IDatabaseValidator, DatabaseValidator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<BrevoSettings>(configuration.GetSection(BrevoSettings.SectionName));
        services.AddSingleton<IAppSettings, AppSettings>();
        services.AddHttpClient<IEmailService, BrevoEmailService>();

        services.Configure<GoogleOAuthSettings>(configuration.GetSection(GoogleOAuthSettings.SectionName));
        services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();

        services.Configure<CedulaScraperSettings>(configuration.GetSection(CedulaScraperSettings.SectionName));
        services.AddHttpClient<ICedulaVerificationService, CedulaVerificationService>(client =>
        {
            client.BaseAddress = new Uri(configuration[$"{CedulaScraperSettings.SectionName}:BaseUrl"]
                ?? "http://cedula-scraper:3000");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    private static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSettings jwtSettings = new();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);

        services.AddSingleton(Options.Create(jwtSettings));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("access_token", out string? token))
                            context.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

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

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddRepositories();
        services
          .AddHealthChecks()
          .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

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

