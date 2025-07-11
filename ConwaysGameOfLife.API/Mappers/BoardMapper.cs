using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.API.Mappers;

public static class BoardMapper
{
    public static BoardResponse ToResponse(this Board board)
    {
        return new BoardResponse
        {
            BoardId = board.Id.Value,
            AliveCells = board.AliveCells.Select(c => new CellCoordinateDto { X = c.X, Y = c.Y }),
            Generation = board.Generation,
            MaxDimension = board.MaxDimension,
            CreatedAt = board.CreatedAt,
            LastUpdatedAt = board.LastUpdatedAt,
            IsEmpty = board.IsEmpty,
            AliveCellCount = board.AliveCells.Count
        };
    }

    public static IEnumerable<CellCoordinate> ToCoordinates(this IEnumerable<CellCoordinateDto> dtos)
    {
        return dtos.Select(dto => dto.ToDomain());
    }
}