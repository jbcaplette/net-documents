using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConwaysGameOfLife.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly GameOfLifeDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(GameOfLifeDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Performing database health check");
            
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                _logger.LogDebug("Database health check passed");
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            else
            {
                _logger.LogWarning("Database health check failed - cannot connect");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}