using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;
using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ConwaysGameOfLife.Infrastructure.Extensions;

public static class PerformanceExtensions
{
    /// <summary>
    /// Bulk insert board histories for better performance
    /// </summary>
    public static async Task BulkInsertBoardHistoriesAsync(
        this GameOfLifeDbContext context, 
        IEnumerable<BoardHistory> boardHistories)
    {
        var entities = boardHistories.Select(bh => 
        {
            var entity = new BoardHistoryEntity
            {
                BoardId = bh.BoardId,
                Generation = bh.Generation,
                StateHash = bh.StateHash,
                CreatedAt = bh.CreatedAt
            };
            entity.SetAliveCells(bh.AliveCells);
            return entity;
        });

        // Disable auto-detect changes for better bulk insert performance
        var originalAutoDetectChanges = context.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            await context.BoardHistories.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }
    }

    /// <summary>
    /// Optimized method to check for cycles without loading full entities
    /// </summary>
    public static async Task<bool> HasStateHashInRecentGenerationsAsync(
        this GameOfLifeDbContext context,
        BoardId boardId,
        string stateHash,
        int maxGenerationsBack)
    {
        var maxGeneration = await context.BoardHistories
            .Where(bh => bh.BoardId == boardId)
            .MaxAsync(bh => (int?)bh.Generation) ?? 0;

        var minGeneration = Math.Max(0, maxGeneration - maxGenerationsBack);

        return await context.BoardHistories
            .Where(bh => bh.BoardId == boardId && 
                        bh.StateHash == stateHash && 
                        bh.Generation >= minGeneration)
            .AnyAsync();
    }

    /// <summary>
    /// Clean up old board history entries to prevent database bloat
    /// </summary>
    public static async Task CleanupOldHistoryAsync(
        this GameOfLifeDbContext context,
        BoardId boardId,
        int keepLastNGenerations = 1000)
    {
        var maxGeneration = await context.BoardHistories
            .Where(bh => bh.BoardId == boardId)
            .MaxAsync(bh => (int?)bh.Generation) ?? 0;

        var cutoffGeneration = maxGeneration - keepLastNGenerations;
        
        if (cutoffGeneration > 0)
        {
            await context.BoardHistories
                .Where(bh => bh.BoardId == boardId && bh.Generation < cutoffGeneration)
                .ExecuteDeleteAsync();
        }
    }
}
