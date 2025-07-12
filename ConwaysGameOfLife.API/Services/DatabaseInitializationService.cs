using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConwaysGameOfLife.API.Services;

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IEnvironmentService _environmentService;

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger,
        IEnvironmentService environmentService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environmentService = environmentService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();
        
        try
        {
            _logger.LogInformation("Ensuring database is created...");
            
            // Use migrations in production, EnsureCreated in development
            if (_environmentService.IsDevelopment)
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("Database ensured in development mode");
            }
            else
            {
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database migrated in production mode");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure database is created");
            throw;
        }
    }
}
