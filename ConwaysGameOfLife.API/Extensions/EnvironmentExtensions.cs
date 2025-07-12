using ConwaysGameOfLife.Domain.Services;

namespace ConwaysGameOfLife.API.Extensions;

/// <summary>
/// Extension methods for configuring environment services
/// </summary>
public static class EnvironmentExtensions
{
    /// <summary>
    /// Adds environment service to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEnvironmentService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEnvironmentService>(provider => new EnvironmentService(configuration));
        return services;
    }

    /// <summary>
    /// Adds environment service to the dependency injection container using environment variables only
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEnvironmentService(this IServiceCollection services)
    {
        services.AddSingleton<IEnvironmentService>(provider => new EnvironmentService());
        return services;
    }
}
