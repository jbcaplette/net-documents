using ConwaysGameOfLife.API.Extensions;
using Serilog;

namespace ConwaysGameOfLife.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog early to capture startup logs
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging with Serilog
        builder.ConfigureLogging();

        try
        {
            Log.Information("Starting Conway's Game of Life API");
            
            // Cache the development environment check to ensure consistency
            var isDevelopment = builder.Environment.IsDevelopment();
            
            // Add services to the container
            builder.Services.AddApiServices(builder.Configuration);

            // Add Swagger services only in development
            // Note: Services must be registered before building the application
            if (isDevelopment)
            {
                builder.Services.AddSwaggerServices();
                Log.Information("Swagger services registered for development environment");
            }

            var app = builder.Build();

            // Configure request logging
            app.ConfigureRequestLogging();

            // Configure Swagger middleware in the HTTP request pipeline
            // Note: Middleware must be configured after building the application
            if (isDevelopment)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                Log.Information("Swagger UI enabled for development environment");
            }

            app.UseHttpsRedirection();

            // Map API endpoints
            app.MapBoardEndpoints();

            Log.Information("Conway's Game of Life API configured successfully");
            
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

