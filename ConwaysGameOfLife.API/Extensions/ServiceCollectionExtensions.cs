using ConwaysGameOfLife.API.Configuration;
using ConwaysGameOfLife.Domain.Configuration;
using ConwaysGameOfLife.API.Services;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Infrastructure;
using ConwaysGameOfLife.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ConwaysGameOfLife.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add environment service first so it's available for other services
        services.AddEnvironmentService(configuration);
        
        // Configure strongly-typed settings
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<ConnectionPoolingSettings>(configuration.GetSection(ConnectionPoolingSettings.SectionName));
        services.Configure<LoggingSettings>(configuration.GetSection(LoggingSettings.SectionName));
        services.Configure<GameOfLifeSettings>(configuration.GetSection(GameOfLifeSettings.SectionName));

        // Add telemetry
        services.AddTelemetry(configuration);

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<UploadBoardRequestValidator>();

        // Add Infrastructure dependencies (repositories, services, database)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string is required");
        
        services.AddInfrastructure(connectionString, configuration);

        // Add database initialization service
        services.AddHostedService<DatabaseInitializationService>();

        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        // Add endpoints API explorer and Swagger for development
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        return services;
    }
}