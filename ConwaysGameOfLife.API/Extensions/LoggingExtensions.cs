using ConwaysGameOfLife.API.Configuration;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

namespace ConwaysGameOfLife.API.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Configure Serilog
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .ConfigureForEnvironment(environment)
            .CreateLogger();

        // Replace default logging with Serilog
        builder.Host.UseSerilog();

        return builder;
    }

    private static LoggerConfiguration ConfigureForEnvironment(this LoggerConfiguration loggerConfig, IWebHostEnvironment environment)
    {
        var baseConfig = loggerConfig
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "ConwaysGameOfLife.API")
            .Enrich.WithProperty("Version", GetApplicationVersion());

        if (environment.IsDevelopment())
        {
            return baseConfig
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/development/conways-game-of-life-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
        else
        {
            return baseConfig
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/production/conways-game-of-life-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 50 * 1024 * 1024, // 50MB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
    }

    private static string GetApplicationVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown";
    }

    public static WebApplication ConfigureRequestLogging(this WebApplication app)
    {
        // Configure Serilog request logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = GetLogLevel;
            options.EnrichDiagnosticContext = EnrichFromRequest;
        });

        return app;
    }

    private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex)
    {
        if (ex != null) return LogEventLevel.Error;

        return ctx.Response.StatusCode switch
        {
            >= 500 => LogEventLevel.Error,
            >= 400 => LogEventLevel.Warning,
            _ => LogEventLevel.Information
        };
    }

    private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString() ?? "Unknown");
        diagnosticContext.Set("CorrelationId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
        
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.Identity.Name ?? "Unknown");
        }

        // Add custom properties for Game of Life specific context
        if (httpContext.Request.RouteValues.TryGetValue("boardId", out var boardId))
        {
            diagnosticContext.Set("BoardId", boardId ?? "Unknown");
        }
    }
}

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure telemetry settings
        services.Configure<TelemetrySettings>(configuration.GetSection("Telemetry"));

        // Add custom telemetry services here if needed
        // For now, we'll rely on Serilog for telemetry

        return services;
    }
}