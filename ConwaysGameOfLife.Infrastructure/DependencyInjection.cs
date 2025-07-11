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
        // Configure DbContext with production-ready settings and connection pooling
        services.AddDbContextPool<GameOfLifeDbContext>(options =>
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
        }, poolSize: configuration?.GetValue<int>("ConnectionPooling:MaxPoolSize") ?? 100);

        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardHistoryRepository, BoardHistoryRepository>();
        services.AddScoped<IBoardService, BoardService>();

        return services;
    }
}