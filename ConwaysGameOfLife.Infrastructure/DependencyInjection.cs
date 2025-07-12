using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Infrastructure.Persistence;
using ConwaysGameOfLife.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConwaysGameOfLife.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        string connectionString, 
        IConfiguration? configuration = null)
    {
        // Determine if we should use connection pooling (avoid in test environments)
        var environmentService = new EnvironmentService(configuration);
        var useConnectionPooling = configuration?.GetValue<bool>("Database:UseConnectionPooling") ?? true;
        var isTestEnvironment = environmentService.IsTest ||
                               connectionString.Contains("InMemoryDatabase", StringComparison.OrdinalIgnoreCase);

        if (useConnectionPooling && !isTestEnvironment)
        {
            // Configure DbContext with connection pooling for production
            services.AddDbContextPool<GameOfLifeDbContext>(options =>
            {
                ConfigureDbContextOptions(options, connectionString, configuration);
            }, poolSize: configuration?.GetValue<int>("ConnectionPooling:MaxPoolSize") ?? 100);
        }
        else
        {
            // Configure DbContext without pooling for testing or when explicitly disabled
            services.AddDbContext<GameOfLifeDbContext>(options =>
            {
                ConfigureDbContextOptions(options, connectionString, configuration);
            });
        }

        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardHistoryRepository, BoardHistoryRepository>();
        services.AddScoped<IBoardService, BoardService>();

        return services;
    }

    private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, 
        string connectionString, 
        IConfiguration? configuration)
    {
        var environmentService = new EnvironmentService(configuration);
        
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            var commandTimeout = configuration?.GetValue<int>("Database:CommandTimeout") ?? 30;
            sqliteOptions.CommandTimeout(commandTimeout);
        });

        // Performance optimizations
        options.EnableSensitiveDataLogging(false); // Disable for production performance
        options.EnableDetailedErrors(false); // Disable for production performance
        
        // Configure based on settings
        if (configuration != null)
        {
            var enableSensitiveDataLogging = configuration.GetValue<bool>("Database:EnableSensitiveDataLogging");
            var enableDetailedErrors = configuration.GetValue<bool>("Database:EnableDetailedErrors");

            if (enableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            // Only enable logging in development using the environment service
            if (environmentService.ShouldEnableLogging)
            {
                options.LogTo(message => 
                {
                    System.Diagnostics.Debug.WriteLine(message);
                }, LogLevel.Information);
            }
        }

        // Performance configurations
        options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SensitiveDataLoggingEnabledWarning));
    }
}