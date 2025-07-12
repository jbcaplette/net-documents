using ConwaysGameOfLife.Domain.Services;

namespace ConwaysGameOfLife.Tests.TestHelpers;

/// <summary>
/// Test implementation of IEnvironmentService that always returns IsTest = true
/// </summary>
public class TestEnvironmentService : IEnvironmentService
{
    public string EnvironmentName => "Test";
    public bool IsTest => true;
    public bool IsDevelopment => false;
    public bool IsProduction => false;
    public bool IsStaging => false;
    public bool ShouldEnableLogging => false;
    public bool ShouldShowDetailedErrors => true;
    public bool ShouldEnableSensitiveDataLogging => false;

    public bool IsEnvironment(string environmentName)
    {
        return string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase);
    }
}
