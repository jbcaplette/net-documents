namespace ConwaysGameOfLife.Domain.ValueObjects;

public record CellCoordinate
{
    public int X { get; init; }
    public int Y { get; init; }

    public CellCoordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public IEnumerable<CellCoordinate> GetNeighbors()
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the cell itself
                yield return new CellCoordinate(X + dx, Y + dy);
            }
        }
    }
}