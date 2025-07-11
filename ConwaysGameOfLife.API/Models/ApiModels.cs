using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.API.Models;

// REPR Pattern - Request/Response models

public record UploadBoardRequest
{
    public required IEnumerable<CellCoordinateDto> AliveCells { get; init; }
    public int MaxDimension { get; init; } = 1000;
}

public record CellCoordinateDto
{
    public required int X { get; init; }
    public required int Y { get; init; }
    
    public CellCoordinate ToDomain() => new(X, Y);
}

public record BoardResponse
{
    public required Guid BoardId { get; init; }
    public required IEnumerable<CellCoordinateDto> AliveCells { get; init; }
    public required int Generation { get; init; }
    public required int MaxDimension { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastUpdatedAt { get; init; }
    public required bool IsEmpty { get; init; }
    public required int AliveCellCount { get; init; }
}

public record GetNextStateRequest
{
    public required Guid BoardId { get; init; }
}

public record GetNStatesAheadRequest
{
    public required Guid BoardId { get; init; }
    public required int Generations { get; init; }
}

public record GetFinalStateRequest
{
    public required Guid BoardId { get; init; }
    public int MaxIterations { get; init; } = 1000;
    public int StableStateThreshold { get; init; } = 20;
}

public record ErrorResponse
{
    public required string Message { get; init; }
    public required string ErrorCode { get; init; }
    public IEnumerable<string>? ValidationErrors { get; init; }
    public required DateTime Timestamp { get; init; }
}