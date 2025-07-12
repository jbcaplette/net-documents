using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Tests.Domain.Entities;

public class BoardTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateBoard()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 1)
        };
        var maxDimension = 100;

        // Act
        var board = new Board(boardId, aliveCells, maxDimension);

        // Assert
        board.Id.Should().Be(boardId);
        board.AliveCells.Should().BeEquivalentTo(aliveCells);
        board.MaxDimension.Should().Be(maxDimension);
        board.Generation.Should().Be(0);
        board.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyAliveCells_ShouldCreateEmptyBoard()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var aliveCells = Enumerable.Empty<CellCoordinate>();
        var maxDimension = 100;

        // Act
        var board = new Board(boardId, aliveCells, maxDimension);

        // Assert
        board.AliveCells.Should().BeEmpty();
        board.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidMaxDimension_ShouldThrowArgumentException(int invalidMaxDimension)
    {
        // Arrange
        var boardId = BoardId.NewId();
        var aliveCells = new[] { new CellCoordinate(1, 1) };

        // Act & Assert
        var action = () => new Board(boardId, aliveCells, invalidMaxDimension);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Max dimension must be positive*");
    }

    [Fact]
    public void Constructor_WithCellsOutsideBoundary_ShouldThrowArgumentException()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var maxDimension = 10;
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(10, 10) // Outside boundary (max is 9)
        };

        // Act & Assert
        var action = () => new Board(boardId, aliveCells, maxDimension);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*outside the maximum dimension*");
    }

    [Fact]
    public void NextGeneration_WithBlinkerPattern_ShouldOscillate()
    {
        // Arrange - Vertical blinker pattern
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(5, 4),
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6)
        };
        var board = new Board(boardId, aliveCells, 20);

        // Act - Generate next state (should become horizontal)
        var nextBoard = board.NextGeneration();

        // Assert
        nextBoard.Generation.Should().Be(1);
        nextBoard.AliveCells.Should().BeEquivalentTo(new[]
        {
            new CellCoordinate(4, 5),
            new CellCoordinate(5, 5),
            new CellCoordinate(6, 5)
        });
    }

    [Fact]
    public void NextGeneration_WithBlockPattern_ShouldRemainStable()
    {
        // Arrange - Block pattern (still life)
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6),
            new CellCoordinate(6, 5),
            new CellCoordinate(6, 6)
        };
        var board = new Board(boardId, aliveCells, 20);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert - Block should remain unchanged
        nextBoard.Generation.Should().Be(1);
        nextBoard.AliveCells.Should().BeEquivalentTo(aliveCells);
    }

    [Fact]
    public void NextGeneration_WithGliderPattern_ShouldMove()
    {
        // Arrange - Glider pattern
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(5, 6),
            new CellCoordinate(6, 7),
            new CellCoordinate(7, 5),
            new CellCoordinate(7, 6),
            new CellCoordinate(7, 7)
        };
        var board = new Board(boardId, aliveCells, 20);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert - Glider should have moved/changed
        nextBoard.Generation.Should().Be(1);
        nextBoard.AliveCells.Should().NotBeEquivalentTo(aliveCells);
        nextBoard.AliveCells.Count.Should().Be(5); // Glider maintains 5 cells
    }

    [Fact]
    public void NextGeneration_WithIsolatedCell_ShouldDie()
    {
        // Arrange - Single isolated cell
        var boardId = BoardId.NewId();
        var aliveCells = new[] { new CellCoordinate(5, 5) };
        var board = new Board(boardId, aliveCells, 20);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert - Cell should die due to loneliness
        nextBoard.Generation.Should().Be(1);
        nextBoard.AliveCells.Should().BeEmpty();
        nextBoard.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NextGeneration_WithOvercrowdedCells_ShouldKillCells()
    {
        // Arrange - Pattern where center cell has 4+ neighbors (overcrowded)
        var boardId = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(4, 4),
            new CellCoordinate(4, 5),
            new CellCoordinate(4, 6),
            new CellCoordinate(5, 4),
            new CellCoordinate(5, 5), // Center cell - will be overcrowded
            new CellCoordinate(5, 6),
            new CellCoordinate(6, 4),
            new CellCoordinate(6, 5),
            new CellCoordinate(6, 6)
        };
        var board = new Board(boardId, aliveCells, 20);

        // Act
        var nextBoard = board.NextGeneration();

        // Assert - Center cell should die due to overcrowding
        nextBoard.AliveCells.Should().NotContain(new CellCoordinate(5, 5));
    }

    [Fact]
    public void IsEquivalentTo_WithSameAliveCells_ShouldReturnTrue()
    {
        // Arrange
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 1)
        };
        var board1 = new Board(boardId1, aliveCells, 100);
        var board2 = new Board(boardId2, aliveCells, 100);

        // Act & Assert
        board1.IsEquivalentTo(board2).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalentTo_WithDifferentAliveCells_ShouldReturnFalse()
    {
        // Arrange
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var aliveCells1 = new[] { new CellCoordinate(1, 1) };
        var aliveCells2 = new[] { new CellCoordinate(2, 2) };
        var board1 = new Board(boardId1, aliveCells1, 100);
        var board2 = new Board(boardId2, aliveCells2, 100);

        // Act & Assert
        board1.IsEquivalentTo(board2).Should().BeFalse();
    }

    [Fact]
    public void GetStateHash_WithSameCells_ShouldReturnSameHash()
    {
        // Arrange
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 1)
        };
        var board1 = new Board(boardId1, aliveCells, 100);
        var board2 = new Board(boardId2, aliveCells, 100);

        // Act & Assert
        board1.GetStateHash().Should().Be(board2.GetStateHash());
    }

    [Fact]
    public void GetStateHash_WithDifferentCells_ShouldReturnDifferentHash()
    {
        // Arrange
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();
        var aliveCells1 = new[] { new CellCoordinate(1, 1) };
        var aliveCells2 = new[] { new CellCoordinate(2, 2) };
        var board1 = new Board(boardId1, aliveCells1, 100);
        var board2 = new Board(boardId2, aliveCells2, 100);

        // Act & Assert
        board1.GetStateHash().Should().NotBe(board2.GetStateHash());
    }
}