using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Domain.ValueObjects;
using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ConwaysGameOfLife.Infrastructure.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly GameOfLifeDbContext _context;

    public BoardRepository(GameOfLifeDbContext context)
    {
        _context = context;
    }

    public async Task<Board?> GetByIdAsync(BoardId boardId)
    {
        var entity = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (entity == null) return null;

        // Use factory method to reconstruct Board from persistence
        return Board.FromPersistence(
            entity.Id, 
            entity.GetAliveCells(), 
            entity.MaxDimension, 
            entity.Generation, 
            entity.CreatedAt,
            entity.LastUpdatedAt);
    }

    public async Task SaveAsync(Board board)
    {
        var existingEntity = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == board.Id);

        if (existingEntity == null)
        {
            var newEntity = new BoardEntity
            {
                Id = board.Id,
                Generation = board.Generation,
                MaxDimension = board.MaxDimension,
                CreatedAt = board.CreatedAt,
                LastUpdatedAt = board.LastUpdatedAt
            };
            newEntity.SetAliveCells(board.AliveCells);

            _context.Boards.Add(newEntity);
        }
        else
        {
            // Prepare the JSON outside of the expression tree
            var aliveCellsJson = JsonConvert.SerializeObject(
                board.AliveCells.Select(c => new { X = c.X, Y = c.Y }).ToArray());
            
            // Use ExecuteUpdate for better performance on updates
            await _context.Boards
                .Where(b => b.Id == board.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(b => b.Generation, board.Generation)
                    .SetProperty(b => b.LastUpdatedAt, DateTime.UtcNow)
                    .SetProperty(b => b.AliveCellsJson, aliveCellsJson));
            return; // Skip SaveChangesAsync when using ExecuteUpdate
        }

        await _context.SaveChangesAsync();
    }

    public async Task SaveWithHistoryAsync(Board board, BoardHistory boardHistory)
    {
        var existingEntity = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == board.Id);

        if (existingEntity == null)
        {
            var newEntity = new BoardEntity
            {
                Id = board.Id,
                Generation = board.Generation,
                MaxDimension = board.MaxDimension,
                CreatedAt = board.CreatedAt,
                LastUpdatedAt = board.LastUpdatedAt
            };
            newEntity.SetAliveCells(board.AliveCells);

            _context.Boards.Add(newEntity);
        }
        else
        {
            // For updates, we need to update the existing entity instead of using ExecuteUpdate
            // because we want to include the history in the same transaction
            existingEntity.Generation = board.Generation;
            existingEntity.LastUpdatedAt = DateTime.UtcNow;
            existingEntity.SetAliveCells(board.AliveCells);
            _context.Boards.Update(existingEntity);
        }

        // Add the board history
        var historyEntity = new BoardHistoryEntity
        {
            BoardId = boardHistory.BoardId,
            Generation = boardHistory.Generation,
            StateHash = boardHistory.StateHash,
            CreatedAt = boardHistory.CreatedAt
        };
        historyEntity.SetAliveCells(boardHistory.AliveCells);
        _context.BoardHistories.Add(historyEntity);

        // Save both board and history atomically in a single transaction
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(BoardId boardId)
    {
        return await _context.Boards.AnyAsync(b => b.Id == boardId);
    }
}

public class BoardHistoryRepository : IBoardHistoryRepository
{
    private readonly GameOfLifeDbContext _context;

    public BoardHistoryRepository(GameOfLifeDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(BoardHistory boardHistory)
    {
        var entity = new BoardHistoryEntity
        {
            BoardId = boardHistory.BoardId,
            Generation = boardHistory.Generation,
            StateHash = boardHistory.StateHash,
            CreatedAt = boardHistory.CreatedAt
        };
        entity.SetAliveCells(boardHistory.AliveCells);

        _context.BoardHistories.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task SaveBatchAsync(IEnumerable<BoardHistory> boardHistories)
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
        }).ToList();

        _context.BoardHistories.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<BoardHistory>> GetHistoryAsync(BoardId boardId)
    {
        var entities = await _context.BoardHistories
            .Where(bh => bh.BoardId == boardId)
            .OrderBy(bh => bh.Generation)
            .AsNoTracking()
            .ToListAsync();

        return entities.Select(e => new BoardHistory(e.BoardId, e.Generation, e.GetAliveCells().ToHashSet()));
    }

    public async Task<BoardHistory?> GetByGenerationAsync(BoardId boardId, int generation)
    {
        var entity = await _context.BoardHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(bh => bh.BoardId == boardId && bh.Generation == generation);

        if (entity == null) return null;

        return new BoardHistory(entity.BoardId, entity.Generation, entity.GetAliveCells().ToHashSet());
    }

    public async Task<bool> HasCycleAsync(BoardId boardId, string stateHash, int generationThreshold)
    {
        return await _context.BoardHistories
            .Where(bh => bh.BoardId == boardId && 
                        bh.StateHash == stateHash && 
                        bh.Generation <= generationThreshold)
            .AnyAsync();
    }
}