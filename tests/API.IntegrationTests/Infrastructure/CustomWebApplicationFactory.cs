using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace API.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("metavix_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
            });
        });

        // Both JWT settings and the EF Core connection string are bound eagerly from IConfiguration
        // before ConfigureAppConfiguration overrides can win. Post-configure both via ConfigureServices.
        builder.ConfigureServices(services =>
        {
            // Replace DbContextFactory so all repositories hit the test container, not the dev DB.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(IDbContextFactory<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext))
                .ToList();
            toRemove.ForEach(d => services.Remove(d));

            services.AddDbContextFactory<AppDbContext>(opts =>
                opts.UseNpgsql(_dbContainer.GetConnectionString())
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            // Override JWT validation parameters — user-secrets bind the secret before we can override config.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = JwtTestHelper.TestIssuer,
                    ValidAudience            = JwtTestHelper.TestAudience,
                    ClockSkew                = TimeSpan.Zero,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtTestHelper.TestSecret)),
                };
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    private async Task MigrateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }
}
