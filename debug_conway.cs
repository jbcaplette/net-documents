using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;
using System;
using System.Linq;

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
    new CellCoordinate(1, 0),
    new CellCoordinate(3, 1),
    new CellCoordinate(0, 2),
    new CellCoordinate(1, 2),
    new CellCoordinate(4, 2),
    new CellCoordinate(5, 2),
    new CellCoordinate(6, 2)
}, 100);

Console.WriteLine($"Initial Acorn: {acorn.AliveCells.Count} cells");
for (int i = 0; i < 5; i++)
{
    acorn = acorn.NextGeneration();
    Console.WriteLine($"Gen {i+1}: {acorn.AliveCells.Count} cells");
    if (acorn.AliveCells.Count == 0)
    {
        Console.WriteLine("All cells died!");
        break;
    }
}
