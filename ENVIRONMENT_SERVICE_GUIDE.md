# Environment Service Usage Guide

This guide explains how to use the new `IEnvironmentService` throughout the Conway's Game of Life solution as a replacement for hard-coded environment checks.

## Overview

The `IEnvironmentService` provides a centralized, reusable way to determine the current environment and make environment-specific decisions. It replaces hard-coded checks like:

```csharp
// ❌ Old approach - hard-coded and not reusable
var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
```

```csharp
// ✅ New approach - centralized and reusable
var isDevelopment = environmentService.IsDevelopment;
```

## Interface Definition

```csharp
public interface IEnvironmentService
{
    string EnvironmentName { get; }
    bool IsDevelopment { get; }
    bool IsProduction { get; }
    bool IsTest { get; }
    bool IsStaging { get; }
    bool IsEnvironment(string environmentName);
    bool ShouldEnableLogging { get; }
    bool ShouldShowDetailedErrors { get; }
    bool ShouldEnableSensitiveDataLogging { get; }
}
```

## Registration

The service is automatically registered in the DI container through the `AddApiServices` extension method:

```csharp
// In ServiceCollectionExtensions.cs
services.AddEnvironmentService(configuration);
```

## Usage Examples

### 1. In Controllers or Services (via Dependency Injection)

```csharp
public class SomeController : ControllerBase
{
    private readonly IEnvironmentService _environmentService;

    public SomeController(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public IActionResult GetDebugInfo()
    {
        if (_environmentService.IsDevelopment)
        {
            return Ok("Debug information available");
        }
        return NotFound();
    }
}
```

### 2. In Static Configuration Methods

```csharp
// In static methods where DI is not available
var environmentService = new EnvironmentService(configuration);

if (environmentService.ShouldEnableLogging)
{
    options.LogTo(Console.WriteLine, LogLevel.Information);
}
```

### 3. In Database Configuration (Current Usage)

```csharp
// In DependencyInjection.cs
private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, 
    string connectionString, 
    IConfiguration? configuration)
{
    var environmentService = new EnvironmentService(configuration);
    
    // Use environment-specific logging
    if (environmentService.ShouldEnableLogging)
    {
        options.LogTo(message => 
        {
            System.Diagnostics.Debug.WriteLine(message);
        }, LogLevel.Information);
    }
}
```

### 4. In Hosted Services

```csharp
// In DatabaseInitializationService.cs
public class DatabaseInitializationService : IHostedService
{
    private readonly IEnvironmentService _environmentService;

    public DatabaseInitializationService(IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Use environment-specific database initialization
        if (_environmentService.IsDevelopment)
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
```

## Environment Detection Logic

The service determines the environment in the following order of preference:

1. `ASPNETCORE_ENVIRONMENT` from IConfiguration
2. `DOTNET_ENVIRONMENT` from IConfiguration
3. `ASPNETCORE_ENVIRONMENT` from environment variables
4. `DOTNET_ENVIRONMENT` from environment variables
5. Defaults to "Production" for safety

## Smart Defaults

The service provides intelligent defaults for common scenarios:

- `ShouldEnableLogging`: `true` for Development and Staging
- `ShouldShowDetailedErrors`: `true` for Development and Test
- `ShouldEnableSensitiveDataLogging`: `true` only for Development

## Benefits

1. **Centralized Logic**: All environment checks go through one service
2. **Reusable**: Can be used in any layer of the application
3. **Testable**: Easy to mock for unit tests
4. **Type Safe**: No more string comparisons scattered throughout the code
5. **Configurable**: Can be extended with custom environment-specific logic
6. **Smart Defaults**: Provides sensible defaults for common scenarios

## Testing

For unit tests, you can easily mock the service:

```csharp
var mockEnvironmentService = new Mock<IEnvironmentService>();
mockEnvironmentService.Setup(x => x.IsDevelopment).Returns(true);
mockEnvironmentService.Setup(x => x.ShouldEnableLogging).Returns(false);

// Use the mock in your tests
var service = new YourService(mockEnvironmentService.Object);
```

Or create a test-specific implementation:

```csharp
var testEnvironmentService = new EnvironmentService("Test");
Assert.True(testEnvironmentService.IsTest);
```

## Migration Guide

To replace existing hard-coded environment checks:

1. Inject `IEnvironmentService` into your class constructor
2. Replace string comparisons with the appropriate property:
   - `"Development"` → `environmentService.IsDevelopment`
   - `"Production"` → `environmentService.IsProduction`
   - `"Test"` → `environmentService.IsTest`
3. Use smart defaults where appropriate:
   - Logging checks → `environmentService.ShouldEnableLogging`
   - Error detail checks → `environmentService.ShouldShowDetailedErrors`

This approach makes your code more maintainable, testable, and follows dependency injection best practices.
