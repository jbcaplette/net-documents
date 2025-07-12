using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;
using System.Diagnostics;

namespace ConwaysGameOfLife.Tests.Performance;

public class PerformanceTests
{
    [Fact]
    public void Board_WithLargeNumberOfCells_ShouldPerformWithinReasonableTime()
    {
        // Arrange - Create a board with many alive cells
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        // Create a 50x50 grid of alive cells (2500 cells)
        for (int x = 10; x < 60; x++)
        {
            for (int y = 10; y < 60; y++)
            {
                aliveCells.Add(new CellCoordinate(x, y));
            }
        }

        var board = new Board(boardId, aliveCells, 100);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var nextBoard = board.NextGeneration();
        stopwatch.Stop();

        // Assert - Should complete within reasonable time (adjust as needed)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
        nextBoard.Should().NotBeNull();
    }

    [Fact]
    public void Board_MultipleGenerations_ShouldPerformWithinReasonableTime()
    {
        // Arrange - Glider pattern
        var boardId = BoardId.NewId();
        var glider = new Board(boardId, new[]
        {
            new CellCoordinate(1, 0),
            new CellCoordinate(2, 1),
            new CellCoordinate(0, 2),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 2)
        }, 100);

        var stopwatch = new Stopwatch();

        // Act - Evolve through 100 generations
        stopwatch.Start();
        var current = glider;
        for (int i = 0; i < 100; i++)
        {
            current = current.NextGeneration();
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // 1 second max for 100 generations
        current.Generation.Should().Be(100);
    }

    [Fact]
    public void Board_WithSparsePattern_ShouldOptimizePerformance()
    {
        // Arrange - Sparse pattern with only a few cells in a large board
        var boardId = BoardId.NewId();
        var sparseCells = new[]
        {
            new CellCoordinate(10, 10),
            new CellCoordinate(50, 50),
            new CellCoordinate(90, 90)
        };
        var board = new Board(boardId, sparseCells, 1000); // Large board, few cells

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var nextBoard = board.NextGeneration();
        stopwatch.Stop();

        // Assert - Should be very fast for sparse patterns
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be nearly instant
        nextBoard.Should().NotBeNull();
    }

    [Fact]
    public void CellCoordinate_GetNeighbors_ShouldPerformWithinReasonableTime()
    {
        // Arrange
        var coordinate = new CellCoordinate(50, 50);
        var stopwatch = new Stopwatch();

        // Act - Call GetNeighbors many times
        stopwatch.Start();
        for (int i = 0; i < 10000; i++)
        {
            var neighbors = coordinate.GetNeighbors().ToList();
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should be very fast
    }

    [Fact]
    public void Board_StateHash_ShouldPerformWithinReasonableTime()
    {
        // Arrange - Board with moderate number of cells
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        // Create a pattern with 100 cells
        for (int i = 0; i < 100; i++)
        {
            aliveCells.Add(new CellCoordinate(i % 20, i / 20));
        }

        var board = new Board(boardId, aliveCells, 50);
        var stopwatch = new Stopwatch();

        // Act - Calculate state hash multiple times
        stopwatch.Start();
        for (int i = 0; i < 1000; i++)
        {
            var hash = board.GetStateHash();
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should be fast
    }

    [Fact]
    public void Board_IsEquivalentTo_ShouldPerformWithinReasonableTime()
    {
        // Arrange - Two boards with moderate number of cells
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        for (int i = 0; i < 100; i++)
        {
            aliveCells.Add(new CellCoordinate(i % 20, i / 20));
        }

        var board1 = new Board(boardId1, aliveCells, 50);
        var board2 = new Board(boardId2, aliveCells, 50);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < 1000; i++)
        {
            var isEquivalent = board1.IsEquivalentTo(board2);
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should be fast
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Board_DifferentSizes_ShouldScaleReasonably(int size)
    {
        // Arrange - Create boards of different sizes
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        // Create a square pattern
        for (int x = 5; x < 5 + size; x++)
        {
            for (int y = 5; y < 5 + size; y++)
            {
                if ((x + y) % 2 == 0) // Checkerboard pattern to ensure some complexity
                {
                    aliveCells.Add(new CellCoordinate(x, y));
                }
            }
        }

        var board = new Board(boardId, aliveCells, size + 20);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var nextBoard = board.NextGeneration();
        stopwatch.Stop();

        // Assert - Time should scale reasonably with size
        var maxTimeMs = size * size / 10; // Rough scaling expectation
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(Math.Max(maxTimeMs, 100));
        nextBoard.Should().NotBeNull();
    }

    [Fact]
    public void Board_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        // Create a moderate-sized pattern
        for (int i = 0; i < 1000; i++)
        {
            aliveCells.Add(new CellCoordinate(i % 50, i / 50));
        }

        // Act & Assert - This test mainly ensures the code doesn't crash with large patterns
        // Memory usage testing is more complex and would typically be done with profiling tools
        var board = new Board(boardId, aliveCells, 100);
        
        for (int i = 0; i < 10; i++)
        {
            board = board.NextGeneration();
        }

        // If we get here without OutOfMemoryException, memory usage is reasonable
        board.Should().NotBeNull();
        board.Generation.Should().Be(10);
    }
}