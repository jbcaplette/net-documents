using Microsoft.Extensions.Configuration;

namespace ConwaysGameOfLife.Domain.Services;

/// <summary>
/// Default implementation of IEnvironmentService that uses IConfiguration to determine environment settings.
/// This implementation is framework-agnostic and can be used in any layer of the application.
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private readonly IConfiguration? _configuration;
    private readonly string _environmentName;

    /// <summary>
    /// Initializes a new instance of EnvironmentService with configuration
    /// </summary>
    /// <param name="configuration">The configuration instance to read environment settings from</param>
    public EnvironmentService(IConfiguration? configuration)
    {
        _configuration = configuration;
        _environmentName = GetEnvironmentName();
    }

    /// <summary>
    /// Parameterless constructor that uses environment variables only
    /// </summary>
    public EnvironmentService()
    {
        _configuration = null;
        _environmentName = GetEnvironmentName();
    }

    /// <summary>
    /// Initializes a new instance of EnvironmentService with explicit environment name
    /// </summary>
    /// <param name="environmentName">The explicit environment name to use</param>
    public EnvironmentService(string environmentName)
    {
        _configuration = null;
        _environmentName = environmentName ?? "Production"; // Default to Production for safety
    }

    public string EnvironmentName => _environmentName;

    public bool IsDevelopment => IsEnvironment("Development");

    public bool IsProduction => IsEnvironment("Production");

    public bool IsTest => IsEnvironment("Test");

    public bool IsStaging => IsEnvironment("Staging");

    public bool IsEnvironment(string environmentName)
    {
        return string.Equals(_environmentName, environmentName, StringComparison.OrdinalIgnoreCase);
    }

    public bool ShouldEnableLogging => IsDevelopment || IsStaging;

    public bool ShouldShowDetailedErrors => IsDevelopment || IsTest;

    public bool ShouldEnableSensitiveDataLogging => IsDevelopment;

    /// <summary>
    /// Gets the environment name from various sources in order of preference:
    /// 1. ASPNETCORE_ENVIRONMENT from configuration
    /// 2. DOTNET_ENVIRONMENT from configuration  
    /// 3. ASPNETCORE_ENVIRONMENT from environment variables
    /// 4. DOTNET_ENVIRONMENT from environment variables
    /// 5. Default to "Production"
    /// </summary>
    private string GetEnvironmentName()
    {
        // Try configuration first (allows for overrides in appsettings)
        var envFromConfig = _configuration?["ASPNETCORE_ENVIRONMENT"] ??
                           _configuration?["DOTNET_ENVIRONMENT"];
        
        if (!string.IsNullOrEmpty(envFromConfig))
        {
            return envFromConfig;
        }

        // Fall back to environment variables
        var envFromEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        
        // Default to Production for safety
        return envFromEnvironment ?? "Production";
    }
}
