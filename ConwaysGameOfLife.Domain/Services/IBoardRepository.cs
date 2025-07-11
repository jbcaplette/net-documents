using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Domain.Services;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId boardId);
    Task SaveAsync(Board board);
    Task<bool> ExistsAsync(BoardId boardId);
}

public interface IBoardHistoryRepository
{
    Task SaveAsync(BoardHistory boardHistory);
    Task<IEnumerable<BoardHistory>> GetHistoryAsync(BoardId boardId);
    Task<BoardHistory?> GetByGenerationAsync(BoardId boardId, int generation);
}