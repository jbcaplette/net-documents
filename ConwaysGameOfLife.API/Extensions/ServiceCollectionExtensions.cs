using ConwaysGameOfLife.API.Configuration;
using ConwaysGameOfLife.API.HealthChecks;
using ConwaysGameOfLife.API.Services;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Infrastructure;
using ConwaysGameOfLife.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ConwaysGameOfLife.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure strongly-typed settings
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<ConnectionPoolingSettings>(configuration.GetSection(ConnectionPoolingSettings.SectionName));
        services.Configure<HealthCheckSettings>(configuration.GetSection(HealthCheckSettings.SectionName));
        services.Configure<LoggingSettings>(configuration.GetSection(LoggingSettings.SectionName));

        // Add telemetry
        services.AddTelemetry(configuration);

        // Add endpoints API explorer and Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<UploadBoardRequestValidator>();

        // Add Infrastructure dependencies (repositories, services, database)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string is required");
        
        services.AddInfrastructure(connectionString, configuration);

        // Add Health Checks
        services.AddScoped<DatabaseHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "database", "ready" })
            .AddCheck("api", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "api", "ready" });

        return services;
    }
}