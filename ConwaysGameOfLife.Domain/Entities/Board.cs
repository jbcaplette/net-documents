using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Domain.Entities;

public class Board
{
    private readonly HashSet<CellCoordinate> _aliveCells;
    
    public BoardId Id { get; private set; }
    public IReadOnlySet<CellCoordinate> AliveCells => _aliveCells;
    public int Generation { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public int MaxDimension { get; private set; }

    public Board(BoardId id, IEnumerable<CellCoordinate> aliveCells, int maxDimension = 1000, int generation = 0)
    {
        if (maxDimension <= 0)
            throw new ArgumentException("Max dimension must be positive", nameof(maxDimension));

        Id = id;
        MaxDimension = maxDimension;
        Generation = generation;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        
        _aliveCells = new HashSet<CellCoordinate>();
        
        foreach (var cell in aliveCells)
        {
            ValidateCoordinate(cell);
            _aliveCells.Add(cell);
        }
    }

    // Internal constructor for creating next generation while preserving CreatedAt
    internal Board(BoardId id, IEnumerable<CellCoordinate> aliveCells, int maxDimension, int generation, DateTime createdAt)
        : this(id, aliveCells, maxDimension, generation, createdAt, DateTime.UtcNow)
    {
    }

    // Private constructor for full reconstruction from persistence
    private Board(BoardId id, IEnumerable<CellCoordinate> aliveCells, int maxDimension, int generation, DateTime createdAt, DateTime lastUpdatedAt)
    {
        if (maxDimension <= 0)
            throw new ArgumentException("Max dimension must be positive", nameof(maxDimension));

        Id = id;
        MaxDimension = maxDimension;
        Generation = generation;
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
        
        _aliveCells = new HashSet<CellCoordinate>();
        
        foreach (var cell in aliveCells)
        {
            ValidateCoordinate(cell);
            _aliveCells.Add(cell);
        }
    }

    /// <summary>
    /// Factory method for reconstructing a Board from persistence.
    /// This preserves the original CreatedAt and LastUpdatedAt timestamps.
    /// </summary>
    public static Board FromPersistence(
        BoardId id, 
        IEnumerable<CellCoordinate> aliveCells, 
        int maxDimension, 
        int generation, 
        DateTime createdAt, 
        DateTime lastUpdatedAt)
    {
        return new Board(id, aliveCells, maxDimension, generation, createdAt, lastUpdatedAt);
    }

    public Board NextGeneration()
    {
        var newAliveCells = new HashSet<CellCoordinate>();
        var cellsToCheck = new HashSet<CellCoordinate>();

        // Add all alive cells and their neighbors to check
        foreach (var cell in _aliveCells)
        {
            cellsToCheck.Add(cell);
            foreach (var neighbor in cell.GetNeighbors())
            {
                cellsToCheck.Add(neighbor);
            }
        }

        // Apply Conway's rules
        foreach (var cell in cellsToCheck)
        {
            if (!IsWithinBounds(cell)) continue;

            var aliveNeighbors = CountAliveNeighbors(cell);
            var isCurrentlyAlive = _aliveCells.Contains(cell);

            if (isCurrentlyAlive && (aliveNeighbors == 2 || aliveNeighbors == 3))
            {
                newAliveCells.Add(cell); // Cell survives
            }
            else if (!isCurrentlyAlive && aliveNeighbors == 3)
            {
                newAliveCells.Add(cell); // Cell is born
            }
        }

        return new Board(Id, newAliveCells, MaxDimension, Generation + 1, CreatedAt);
    }

    public bool IsEmpty => !_aliveCells.Any();

    public bool IsEquivalentTo(Board other)
    {
        return _aliveCells.SetEquals(other._aliveCells);
    }

    public string GetStateHash()
    {
        var sortedCells = _aliveCells.OrderBy(c => c.X).ThenBy(c => c.Y);
        var cellString = string.Join(",", sortedCells.Select(c => $"{c.X}:{c.Y}"));
        return cellString.GetHashCode().ToString();
    }

    private int CountAliveNeighbors(CellCoordinate cell)
    {
        return cell.GetNeighbors().Count(neighbor => _aliveCells.Contains(neighbor));
    }

    private void ValidateCoordinate(CellCoordinate coordinate)
    {
        if (!IsWithinBounds(coordinate))
        {
            throw new ArgumentException(
                $"Cell coordinate ({coordinate.X}, {coordinate.Y}) is outside the maximum dimension of {MaxDimension}");
        }
    }

    private bool IsWithinBounds(CellCoordinate coordinate)
    {
        return coordinate.X >= 0 && coordinate.X < MaxDimension &&
               coordinate.Y >= 0 && coordinate.Y < MaxDimension;
    }
}