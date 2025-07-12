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
        var useConnectionPooling = configuration?.GetValue<bool>("Database:UseConnectionPooling") ?? true;
        var isTestEnvironment = configuration?.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Test" ||
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
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            var commandTimeout = configuration?.GetValue<int>("Database:CommandTimeout") ?? 30;
            sqliteOptions.CommandTimeout(commandTimeout);
        });

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

            // Use Serilog for EF Core logging instead of Debug.WriteLine
            options.LogTo(message => 
            {
                // This will be picked up by Serilog through the ILogger infrastructure
                System.Diagnostics.Debug.WriteLine(message);
            }, LogLevel.Information);
        }
    }
}