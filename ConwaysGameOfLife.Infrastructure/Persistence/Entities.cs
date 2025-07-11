using ConwaysGameOfLife.Domain.ValueObjects;
using Newtonsoft.Json;

namespace ConwaysGameOfLife.Infrastructure.Persistence;

public class BoardEntity
{
    public BoardId Id { get; set; } = null!;
    public string AliveCellsJson { get; set; } = string.Empty;
    public int Generation { get; set; }
    public int MaxDimension { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public IEnumerable<CellCoordinate> GetAliveCells()
    {
        if (string.IsNullOrEmpty(AliveCellsJson))
            return Enumerable.Empty<CellCoordinate>();

        var coordinates = JsonConvert.DeserializeObject<CoordinateDto[]>(AliveCellsJson);
        return coordinates?.Select(c => new CellCoordinate(c.X, c.Y)) ?? Enumerable.Empty<CellCoordinate>();
    }

    public void SetAliveCells(IEnumerable<CellCoordinate> aliveCells)
    {
        var coordinates = aliveCells.Select(c => new CoordinateDto { X = c.X, Y = c.Y }).ToArray();
        AliveCellsJson = JsonConvert.SerializeObject(coordinates);
    }

    private record CoordinateDto
    {
        public int X { get; init; }
        public int Y { get; init; }
    }
}

public class BoardHistoryEntity
{
    public BoardId BoardId { get; set; } = null!;
    public int Generation { get; set; }
    public string AliveCellsJson { get; set; } = string.Empty;
    public string StateHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public IEnumerable<CellCoordinate> GetAliveCells()
    {
        if (string.IsNullOrEmpty(AliveCellsJson))
            return Enumerable.Empty<CellCoordinate>();

        var coordinates = JsonConvert.DeserializeObject<CoordinateDto[]>(AliveCellsJson);
        return coordinates?.Select(c => new CellCoordinate(c.X, c.Y)) ?? Enumerable.Empty<CellCoordinate>();
    }

    public void SetAliveCells(IEnumerable<CellCoordinate> aliveCells)
    {
        var coordinates = aliveCells.Select(c => new CoordinateDto { X = c.X, Y = c.Y }).ToArray();
        AliveCellsJson = JsonConvert.SerializeObject(coordinates);
    }

    private record CoordinateDto
    {
        public int X { get; init; }
        public int Y { get; init; }
    }
}