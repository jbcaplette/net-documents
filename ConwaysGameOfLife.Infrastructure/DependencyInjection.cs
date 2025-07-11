using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Infrastructure.Persistence;
using ConwaysGameOfLife.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConwaysGameOfLife.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GameOfLifeDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IBoardHistoryRepository, BoardHistoryRepository>();
        services.AddScoped<IBoardService, BoardService>();

        return services;
    }
}