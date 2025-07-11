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
        // Add endpoints API explorer and Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Configure Newtonsoft.Json for MVC/Controllers
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<UploadBoardRequestValidator>();

        // Add Infrastructure dependencies (repositories, services, database)
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gameoflife.db";
        services.AddInfrastructure(connectionString);

        return services;
    }

    public static async Task<WebApplication> EnsureDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();
        await context.Database.EnsureCreatedAsync();
        return app;
    }
}