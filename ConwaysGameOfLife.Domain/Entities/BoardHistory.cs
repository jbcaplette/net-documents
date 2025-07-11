using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Domain.Entities;

public class BoardHistory
{
    public BoardId BoardId { get; private set; }
    public int Generation { get; private set; }
    public string StateHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlySet<CellCoordinate> AliveCells { get; private set; }

    public BoardHistory(BoardId boardId, int generation, IReadOnlySet<CellCoordinate> aliveCells)
    {
        BoardId = boardId;
        Generation = generation;
        AliveCells = aliveCells;
        StateHash = GenerateStateHash(aliveCells);
        CreatedAt = DateTime.UtcNow;
    }

    private static string GenerateStateHash(IReadOnlySet<CellCoordinate> aliveCells)
    {
        var sortedCells = aliveCells.OrderBy(c => c.X).ThenBy(c => c.Y);
        var cellString = string.Join(",", sortedCells.Select(c => $"{c.X}:{c.Y}"));
        return cellString.GetHashCode().ToString();
    }
}