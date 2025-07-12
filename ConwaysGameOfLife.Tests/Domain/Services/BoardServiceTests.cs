using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ConwaysGameOfLife.Tests.Domain.Services;

public class BoardServiceTests
{
    private readonly Mock<IBoardRepository> _mockBoardRepository;
    private readonly Mock<IBoardHistoryRepository> _mockBoardHistoryRepository;
    private readonly Mock<ILogger<BoardService>> _mockLogger;
    private readonly BoardService _boardService;

    public BoardServiceTests()
    {
        _mockBoardRepository = new Mock<IBoardRepository>();
        _mockBoardHistoryRepository = new Mock<IBoardHistoryRepository>();
        _mockLogger = new Mock<ILogger<BoardService>>();
        _boardService = new BoardService(_mockBoardRepository.Object, _mockBoardHistoryRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateBoardAsync_WithValidInput_ShouldCreateAndSaveBoard()
    {
        // Arrange
        var aliveCells = new[]
        {
            new CellCoordinate(1, 1),
            new CellCoordinate(1, 2),
            new CellCoordinate(2, 1)
        };
        var maxDimension = 100;

        // Act
        var result = await _boardService.CreateBoardAsync(aliveCells, maxDimension);

        // Assert
        result.Should().NotBeNull();
        result.AliveCells.Should().BeEquivalentTo(aliveCells);
        result.MaxDimension.Should().Be(maxDimension);
        result.Generation.Should().Be(0);

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.Once);
        _mockBoardHistoryRepository.Verify(x => x.SaveAsync(It.IsAny<BoardHistory>()), Times.Once);
    }

    [Fact]
    public async Task CreateBoardAsync_WhenRepositoryThrows_ShouldRethrowException()
    {
        // Arrange
        var aliveCells = new[] { new CellCoordinate(1, 1) };
        _mockBoardRepository.Setup(x => x.SaveAsync(It.IsAny<Board>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var action = async () => await _boardService.CreateBoardAsync(aliveCells);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task GetNextStateAsync_WithValidBoardId_ShouldReturnNextGeneration()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var originalBoard = new Board(boardId, new[] { new CellCoordinate(1, 1) }, 100);
        
        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(originalBoard);

        // Act
        var result = await _boardService.GetNextStateAsync(boardId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.Generation.Should().Be(1);

        _mockBoardRepository.Verify(x => x.GetByIdAsync(boardId), Times.Once);
        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.Once);
        _mockBoardHistoryRepository.Verify(x => x.SaveAsync(It.IsAny<BoardHistory>()), Times.Once);
    }

    [Fact]
    public async Task GetNextStateAsync_WithNonExistentBoard_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var boardId = BoardId.NewId();
        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync((Board?)null);

        // Act & Assert
        var action = async () => await _boardService.GetNextStateAsync(boardId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Board with ID {boardId.Value} not found");
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_WithValidInput_ShouldReturnCorrectGeneration()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var originalBoard = new Board(boardId, new[]
        {
            new CellCoordinate(5, 4),
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6)
        }, 100);
        var generations = 5;

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(originalBoard);

        // Act
        var result = await _boardService.GetStateAfterGenerationsAsync(boardId, generations);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.Generation.Should().Be(generations);

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.Once);
        _mockBoardHistoryRepository.Verify(x => x.SaveAsync(It.IsAny<BoardHistory>()), Times.Exactly(generations));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetStateAfterGenerationsAsync_WithNegativeGenerations_ShouldThrowArgumentException(int negativeGenerations)
    {
        // Arrange
        var boardId = BoardId.NewId();

        // Act & Assert
        var action = async () => await _boardService.GetStateAfterGenerationsAsync(boardId, negativeGenerations);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Generations must be non-negative*");
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_WithZeroGenerations_ShouldReturnOriginalBoard()
    {
        // Arrange
        var boardId = BoardId.NewId();
        var originalBoard = new Board(boardId, new[] { new CellCoordinate(1, 1) }, 100);

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(originalBoard);

        // Act
        var result = await _boardService.GetStateAfterGenerationsAsync(boardId, 0);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.Generation.Should().Be(0);

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.Once);
        _mockBoardHistoryRepository.Verify(x => x.SaveAsync(It.IsAny<BoardHistory>()), Times.Never);
    }

    [Fact]
    public async Task GetFinalStateAsync_WithStablePattern_ShouldDetectStability()
    {
        // Arrange - Block pattern (stable)
        var boardId = BoardId.NewId();
        var blockBoard = new Board(boardId, new[]
        {
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6),
            new CellCoordinate(6, 5),
            new CellCoordinate(6, 6)
        }, 100);

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(blockBoard);

        // Act
        var result = await _boardService.GetFinalStateAsync(boardId, maxIterations: 100, stableStateThreshold: 5);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        // Block pattern should stabilize immediately
        result.Generation.Should().BeGreaterThan(0);

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetFinalStateAsync_WithEmptyBoard_ShouldReturnEmptyState()
    {
        // Arrange - Empty board
        var boardId = BoardId.NewId();
        var emptyBoard = new Board(boardId, Enumerable.Empty<CellCoordinate>(), 100);

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(emptyBoard);

        // Act
        var result = await _boardService.GetFinalStateAsync(boardId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.IsEmpty.Should().BeTrue();

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.Once);
    }

    [Fact]
    public async Task GetFinalStateAsync_WithOscillatingPattern_ShouldDetectCycle()
    {
        // Arrange - Blinker pattern (oscillates with period 2)
        var boardId = BoardId.NewId();
        var blinkerBoard = new Board(boardId, new[]
        {
            new CellCoordinate(5, 4),
            new CellCoordinate(5, 5),
            new CellCoordinate(5, 6)
        }, 100);

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(blinkerBoard);

        // Act
        var result = await _boardService.GetFinalStateAsync(boardId, maxIterations: 100, stableStateThreshold: 5);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        // Blinker should be detected as cycling
        result.Generation.Should().BeGreaterThan(0);

        _mockBoardRepository.Verify(x => x.SaveAsync(It.IsAny<Board>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetFinalStateAsync_WithNonStablePattern_ShouldThrowAfterMaxIterations()
    {
        // Arrange - Create a glider pattern that keeps moving and won't stabilize quickly
        var boardId = BoardId.NewId();
        var glider = new Board(boardId, new[]
        {
            // Glider pattern - moves indefinitely in infinite space
            new CellCoordinate(50, 50),  // Center it in a large board
            new CellCoordinate(51, 51),
            new CellCoordinate(49, 52),
            new CellCoordinate(50, 52),
            new CellCoordinate(51, 52)
        }, 200);  // Large board so it won't hit boundaries quickly

        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync(glider);

        // Act & Assert - Use very small maxIterations to force timeout
        var action = async () => await _boardService.GetFinalStateAsync(boardId, maxIterations: 3, stableStateThreshold: 2);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Board did not reach a stable state within 3 iterations");
    }

    [Fact]
    public async Task GetFinalStateAsync_WithNonExistentBoard_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var boardId = BoardId.NewId();
        _mockBoardRepository.Setup(x => x.GetByIdAsync(boardId))
            .ReturnsAsync((Board?)null);

        // Act & Assert
        var action = async () => await _boardService.GetFinalStateAsync(boardId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Board with ID {boardId.Value} not found");
    }
}