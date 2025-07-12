using ConwaysGameOfLife.API.Services;
using ConwaysGameOfLife.Domain.Services;

namespace ConwaysGameOfLife.API.Extensions;

public static class ApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        // Get the environment service to check if we're in test mode
        using var scope = app.Services.CreateScope();
        var environmentService = scope.ServiceProvider.GetService<IEnvironmentService>();
        
        // Skip database initialization in test environments
        if (environmentService?.IsTest == true)
        {
            var logger = scope.ServiceProvider.GetService<ILogger<WebApplication>>();
            logger?.LogInformation("Skipping database initialization in test environment");
            return;
        }

        // Initialize the database
        var dbInitializer = scope.ServiceProvider.GetService<IDatabaseInitializationService>();
        if (dbInitializer != null)
        {
            await dbInitializer.InitializeAsync();
        }
    }
}
