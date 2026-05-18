using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DatabaseValidator : IDatabaseValidator
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseValidator> _logger;

    public DatabaseValidator(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DatabaseValidator> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.OpenConnectionAsync(cancellationToken); // Test connection
            await dbContext.Database.CloseConnectionAsync(); // Close connection
            _logger.LogInformation("Conexión a la base de datos validada correctamente. ✅");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar la conexión a la base de datos. ❌");
            throw;
        }
    }
}