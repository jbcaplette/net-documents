namespace ConwaysGameOfLife.Domain.Services;

/// <summary>
/// Service for environment-related operations and checks.
/// Provides a centralized way to determine the current environment and make environment-specific decisions.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Gets the current environment name (Development, Staging, Production, Test, etc.)
    /// </summary>
    string EnvironmentName { get; }
    
    /// <summary>
    /// Determines if the current environment is Development
    /// </summary>
    bool IsDevelopment { get; }
    
    /// <summary>
    /// Determines if the current environment is Production
    /// </summary>
    bool IsProduction { get; }
    
    /// <summary>
    /// Determines if the current environment is Test
    /// </summary>
    bool IsTest { get; }
    
    /// <summary>
    /// Determines if the current environment is Staging
    /// </summary>
    bool IsStaging { get; }
    
    /// <summary>
    /// Checks if the current environment matches the specified environment name (case-insensitive)
    /// </summary>
    /// <param name="environmentName">The environment name to check against</param>
    /// <returns>True if the current environment matches the specified name</returns>
    bool IsEnvironment(string environmentName);
    
    /// <summary>
    /// Determines if logging should be enabled for the current environment
    /// </summary>
    bool ShouldEnableLogging { get; }
    
    /// <summary>
    /// Determines if detailed error information should be shown for the current environment
    /// </summary>
    bool ShouldShowDetailedErrors { get; }
    
    /// <summary>
    /// Determines if sensitive data logging should be enabled for the current environment
    /// </summary>
    bool ShouldEnableSensitiveDataLogging { get; }
}
