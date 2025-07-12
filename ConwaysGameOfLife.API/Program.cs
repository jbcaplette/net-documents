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
            
            // Add services to the container
            builder.Services.AddApiServices(builder.Configuration);

            var app = builder.Build();

            // Configure request logging
            app.ConfigureRequestLogging();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
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

