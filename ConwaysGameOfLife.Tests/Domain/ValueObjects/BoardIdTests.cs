using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Tests.Domain.ValueObjects;

public class BoardIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateBoardId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var boardId = new BoardId(guid);

        // Assert
        boardId.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act & Assert
        var action = () => new BoardId(emptyGuid);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Board ID cannot be empty*");
    }

    [Fact]
    public void NewId_ShouldCreateUniqueIds()
    {
        // Act
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();

        // Assert
        boardId1.Value.Should().NotBe(Guid.Empty);
        boardId2.Value.Should().NotBe(Guid.Empty);
        boardId1.Should().NotBe(boardId2);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var boardId = BoardId.NewId();

        // Act
        Guid guid = boardId; // Implicit conversion

        // Assert
        guid.Should().Be(boardId.Value);
    }

    [Fact]
    public void ImplicitConversion_FromGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        BoardId boardId = guid; // Implicit conversion

        // Assert
        boardId.Value.Should().Be(guid);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var boardId1 = new BoardId(guid);
        var boardId2 = new BoardId(guid);

        // Act & Assert
        boardId1.Should().Be(boardId2);
        (boardId1 == boardId2).Should().BeTrue();
        boardId1.GetHashCode().Should().Be(boardId2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var boardId1 = BoardId.NewId();
        var boardId2 = BoardId.NewId();

        // Act & Assert
        boardId1.Should().NotBe(boardId2);
        (boardId1 != boardId2).Should().BeTrue();
    }
}