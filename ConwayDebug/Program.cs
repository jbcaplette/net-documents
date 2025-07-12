using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

// Create a simple test
var boardId = BoardId.NewId();

// Test with a simple blinker pattern (should oscillate)
var blinker = new Board(boardId, new[]
{
    new CellCoordinate(1, 0),
    new CellCoordinate(1, 1),
    new CellCoordinate(1, 2)
}, 10);

Console.WriteLine($"Initial: {blinker.AliveCells.Count} cells");
foreach (var cell in blinker.AliveCells)
{
    Console.WriteLine($"  ({cell.X}, {cell.Y})");
}

var gen1 = blinker.NextGeneration();
Console.WriteLine($"Gen 1: {gen1.AliveCells.Count} cells");
foreach (var cell in gen1.AliveCells)
{
    Console.WriteLine($"  ({cell.X}, {cell.Y})");
}

var gen2 = gen1.NextGeneration();
Console.WriteLine($"Gen 2: {gen2.AliveCells.Count} cells");
foreach (var cell in gen2.AliveCells)
{
    Console.WriteLine($"  ({cell.X}, {cell.Y})");
}

// Test Acorn specifically
Console.WriteLine("\n--- Acorn Pattern ---");
var acorn = new Board(BoardId.NewId(), new[]
{
    new CellCoordinate(501, 500),  // Center in 1000x1000 board
    new CellCoordinate(503, 501),
    new CellCoordinate(500, 502),
    new CellCoordinate(501, 502),
    new CellCoordinate(504, 502),
    new CellCoordinate(505, 502),
    new CellCoordinate(506, 502)
}, 1000);  // Much larger board

// Test Pulsar pattern
Console.WriteLine("\n--- Pulsar Pattern ---");
var pulsar = new Board(BoardId.NewId(), new[]
{
    // This is a simplified version of the pulsar pattern
    new CellCoordinate(2, 0), new CellCoordinate(3, 0), new CellCoordinate(4, 0),
    new CellCoordinate(8, 0), new CellCoordinate(9, 0), new CellCoordinate(10, 0),
    new CellCoordinate(0, 2), new CellCoordinate(5, 2), new CellCoordinate(7, 2), new CellCoordinate(12, 2),
    new CellCoordinate(0, 3), new CellCoordinate(5, 3), new CellCoordinate(7, 3), new CellCoordinate(12, 3),
    new CellCoordinate(0, 4), new CellCoordinate(5, 4), new CellCoordinate(7, 4), new CellCoordinate(12, 4),
    new CellCoordinate(2, 5), new CellCoordinate(3, 5), new CellCoordinate(4, 5),
    new CellCoordinate(8, 5), new CellCoordinate(9, 5), new CellCoordinate(10, 5),
    new CellCoordinate(2, 7), new CellCoordinate(3, 7), new CellCoordinate(4, 7),
    new CellCoordinate(8, 7), new CellCoordinate(9, 7), new CellCoordinate(10, 7),
    new CellCoordinate(0, 8), new CellCoordinate(5, 8), new CellCoordinate(7, 8), new CellCoordinate(12, 8),
    new CellCoordinate(0, 9), new CellCoordinate(5, 9), new CellCoordinate(7, 9), new CellCoordinate(12, 9),
    new CellCoordinate(0, 10), new CellCoordinate(5, 10), new CellCoordinate(7, 10), new CellCoordinate(12, 10),
    new CellCoordinate(2, 12), new CellCoordinate(3, 12), new CellCoordinate(4, 12),
    new CellCoordinate(8, 12), new CellCoordinate(9, 12), new CellCoordinate(10, 12)
}, 20);

var originalHash = pulsar.GetStateHash();
Console.WriteLine($"Initial Pulsar: {pulsar.AliveCells.Count} cells, hash: {originalHash}");

var current = pulsar;
for (int i = 0; i < 5; i++)
{
    current = current.NextGeneration();
    var hash = current.GetStateHash();
    Console.WriteLine($"Gen {i+1}: {current.AliveCells.Count} cells, hash: {hash}");
    if (hash == originalHash)
    {
        Console.WriteLine($"  Returned to original state at generation {i+1}!");
    }
}
