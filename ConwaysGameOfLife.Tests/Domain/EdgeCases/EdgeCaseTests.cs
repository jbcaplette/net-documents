using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Tests.Domain.EdgeCases;

public class EdgeCaseTests
{
    [Fact]
    public void Board_WithMaximumDimensionCells_ShouldHandleCorrectly()
    {
        // Arrange - Cells at the maximum boundaries
        var boardId = BoardId.NewId();
        var maxDim = 100;
        var aliveCells = new[]
        {
            new CellCoordinate(0, 0),           // Top-left corner
            new CellCoordinate(maxDim - 1, 0),  // Top-right corner
            new CellCoordinate(0, maxDim - 1),  // Bottom-left corner
            new CellCoordinate(maxDim - 1, maxDim - 1), // Bottom-right corner
            new CellCoordinate(maxDim / 2, maxDim / 2)  // Center
        };

        // Act
        var board = new Board(boardId, aliveCells, maxDim);
        var nextBoard = board.NextGeneration();

        // Assert
        nextBoard.Should().NotBeNull();
        nextBoard.Generation.Should().Be(1);
        // Corner cells should typically die due to insufficient neighbors
        nextBoard.AliveCells.Should().NotContain(new CellCoordinate(0, 0));
        nextBoard.AliveCells.Should().NotContain(new CellCoordinate(maxDim - 1, maxDim - 1));
    }

    [Fact]
    public void Board_WithDuplicateCoordinates_ShouldHandleCorrectly()
    {
        // Arrange - List with duplicate coordinates
        var boardId = BoardId.NewId();
        var aliveCellsWithDuplicates = new[]
        {
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6),
            new CellCoordinate(5, 5), // Duplicate
            new CellCoordinate(6, 5),
            new CellCoordinate(5, 6)  // Another duplicate
        };

        // Act
        var board = new Board(boardId, aliveCellsWithDuplicates, 20);

        // Assert - Should only contain unique coordinates
        board.AliveCells.Should().HaveCount(3);
        board.AliveCells.Should().Contain(new CellCoordinate(5, 5));
        board.AliveCells.Should().Contain(new CellCoordinate(5, 6));
        board.AliveCells.Should().Contain(new CellCoordinate(6, 5));
    }

    [Fact]
    public void Board_WithSingleCellAtBoundary_ShouldHandleCorrectly()
    {
        // Arrange - Single cell at each boundary
        var boardId = BoardId.NewId();
        var maxDim = 10;

        // Test each corner and edge
        var testCases = new[]
        {
            new CellCoordinate(0, 0),           // Top-left corner
            new CellCoordinate(maxDim - 1, 0),  // Top-right corner
            new CellCoordinate(0, maxDim - 1),  // Bottom-left corner
            new CellCoordinate(maxDim - 1, maxDim - 1), // Bottom-right corner
            new CellCoordinate(maxDim / 2, 0),  // Top edge
            new CellCoordinate(maxDim / 2, maxDim - 1), // Bottom edge
            new CellCoordinate(0, maxDim / 2),  // Left edge
            new CellCoordinate(maxDim - 1, maxDim / 2)  // Right edge
        };

        foreach (var cellCoordinate in testCases)
        {
            // Act
            var board = new Board(boardId, new[] { cellCoordinate }, maxDim);
            var nextBoard = board.NextGeneration();

            // Assert - Single boundary cells should die (no neighbors)
            nextBoard.AliveCells.Should().BeEmpty($"Cell at {cellCoordinate.X},{cellCoordinate.Y} should die");
            nextBoard.IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void Board_WithVeryLargeBoundingBox_ShouldHandleCorrectly()
    {
        // Arrange - Cells spread across a very large area
        var boardId = BoardId.NewId();
        var maxDim = 1000;
        var aliveCells = new[]
        {
            new CellCoordinate(0, 0),
            new CellCoordinate(999, 999),
            new CellCoordinate(500, 500)
        };

        // Act
        var board = new Board(boardId, aliveCells, maxDim);
        var nextBoard = board.NextGeneration();

        // Assert
        nextBoard.Should().NotBeNull();
        nextBoard.Generation.Should().Be(1);
        // All isolated cells should die
        nextBoard.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Board_WithComplexPatternNearBoundary_ShouldHandleCorrectly()
    {
        // Arrange - Glider near the boundary that should hit the edge
        var boardId = BoardId.NewId();
        var maxDim = 10;
        var gliderNearEdge = new[]
        {
            new CellCoordinate(7, 0),  // Near top-right
            new CellCoordinate(8, 1),
            new CellCoordinate(6, 2),
            new CellCoordinate(7, 2),
            new CellCoordinate(8, 2)
        };

        var board = new Board(boardId, gliderNearEdge, maxDim);

        // Act - Evolve several generations
        var current = board;
        for (int i = 0; i < 10; i++)
        {
            current = current.NextGeneration();
            // Ensure we don't crash when the glider hits the boundary
            current.Should().NotBeNull();
        }

        // Assert - Pattern should eventually stabilize or disappear at boundary
        current.Generation.Should().Be(10);
    }

    [Fact]
    public void Board_StateHash_WithIdenticalButReorderedCells_ShouldBeEqual()
    {
        // Arrange - Two boards with same cells in different order
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var cells1 = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(2, 2),
            new CellCoordinate(3, 3)
        };
        var cells2 = new[]
        {
            new CellCoordinate(3, 3),
            new CellCoordinate(1, 1),
            new CellCoordinate(2, 2)
        };

        var board1 = new Board(boardId1, cells1, 10);
        var board2 = new Board(boardId2, cells2, 10);

        // Act
        var hash1 = board1.GetStateHash();
        var hash2 = board2.GetStateHash();

        // Assert - Hashes should be equal regardless of input order
        hash1.Should().Be(hash2);
        board1.IsEquivalentTo(board2).Should().BeTrue();
    }

    [Fact]
    public void Board_NextGeneration_PreservesCreatedAtTime()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 1)
        };
        var originalBoard = new Board(boardId, aliveCells, 10);
        var originalCreatedAt = originalBoard.CreatedAt;

        // Act
        var nextBoard = originalBoard.NextGeneration();

        // Assert
        nextBoard.CreatedAt.Should().Be(originalCreatedAt);
        nextBoard.LastUpdatedAt.Should().BeAfter(originalCreatedAt);
        nextBoard.Generation.Should().Be(1);
    }

    [Fact]
    public void Board_WithCellsFormingLargeConnectedComponent_ShouldHandleCorrectly()
    {
        // Arrange - Create a large connected component (line)
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        // Create a horizontal line of 20 cells
        for (int x = 10; x < 30; x++)
        {
            aliveCells.Add(new CellCoordinate(x, 15));
        }

        var board = new Board(boardId, aliveCells, 50);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert
        nextBoard.Should().NotBeNull();
        // Line patterns typically create specific patterns in Conway's Game of Life
        nextBoard.AliveCells.Count.Should().BeGreaterThan(0);
        nextBoard.Generation.Should().Be(1);
    }

    [Fact]
    public void Board_WithAlternatingSparsePattern_ShouldHandleCorrectly()
    {
        // Arrange - Checkerboard-like sparse pattern
        var boardId = BoardId.NewId();
        var aliveCells = new List<CellCoordinate>();
        
        for (int x = 0; x < 20; x += 4)
        {
            for (int y = 0; y < 20; y += 4)
            {
                aliveCells.Add(new CellCoordinate(x, y));
            }
        }

        var board = new Board(boardId, aliveCells, 25);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert
        nextBoard.Should().NotBeNull();
        // Isolated cells should die
        nextBoard.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Board_WithZeroGenerationAfterMultipleGenerations_ShouldMaintainConsistency()
    {
        // Arrange - Start with a blinker
        var boardId = BoardId.NewId();
        var blinker = new[]
        {
            new CellCoordinate(5, 4),
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6)
        };
        var board = new Board(boardId, blinker, 20);

        // Act - Evolve and check that properties remain consistent
        var generation1 = board.NextGeneration();
        var generation2 = generation1.NextGeneration();

        // Assert
        board.Generation.Should().Be(0);
        generation1.Generation.Should().Be(1);
        generation2.Generation.Should().Be(2);
        
        // Should return to original state (blinker period 2)
        generation2.AliveCells.Should().BeEquivalentTo(board.AliveCells);
        
        // All should have same BoardId and CreatedAt
        generation1.Id.Should().Be(board.Id);
        generation2.Id.Should().Be(board.Id);
        generation1.CreatedAt.Should().Be(board.CreatedAt);
        generation2.CreatedAt.Should().Be(board.CreatedAt);
    }
}