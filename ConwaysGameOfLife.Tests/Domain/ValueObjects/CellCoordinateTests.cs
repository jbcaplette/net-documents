using ConwaysGameOfLife.Domain.ValueObjects;

namespace ConwaysGameOfLife.Tests.Domain.ValueObjects;

public class CellCoordinateTests
{
    [Fact]
    public void Constructor_WithValidCoordinates_ShouldCreateCellCoordinate()
    {
        // Arrange & Act
        var coordinate = new CellCoordinate(5, 10);

        // Assert
        coordinate.X.Should().Be(5);
        coordinate.Y.Should().Be(10);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, -10)]
    [InlineData(100, 200)]
    public void Constructor_WithAnyValidIntegers_ShouldCreateCellCoordinate(int x, int y)
    {
        // Act
        var coordinate = new CellCoordinate(x, y);

        // Assert
        coordinate.X.Should().Be(x);
        coordinate.Y.Should().Be(y);
    }

    [Fact]
    public void GetNeighbors_ShouldReturn8Neighbors()
    {
        // Arrange
        var coordinate = new CellCoordinate(5, 5);

        // Act
        var neighbors = coordinate.GetNeighbors().ToList();

        // Assert
        neighbors.Should().HaveCount(8);
        neighbors.Should().BeEquivalentTo(new[]
        {
            new CellCoordinate(4, 4), new CellCoordinate(4, 5), new CellCoordinate(4, 6),
            new CellCoordinate(5, 4),                            new CellCoordinate(5, 6),
            new CellCoordinate(6, 4), new CellCoordinate(6, 5), new CellCoordinate(6, 6)
        });
    }

    [Fact]
    public void GetNeighbors_ForOriginCell_ShouldReturnCorrectNeighbors()
    {
        // Arrange
        var coordinate = new CellCoordinate(0, 0);

        // Act
        var neighbors = coordinate.GetNeighbors().ToList();

        // Assert
        neighbors.Should().HaveCount(8);
        neighbors.Should().BeEquivalentTo(new[]
        {
            new CellCoordinate(-1, -1), new CellCoordinate(-1, 0), new CellCoordinate(-1, 1),
            new CellCoordinate(0, -1),                             new CellCoordinate(0, 1),
            new CellCoordinate(1, -1),  new CellCoordinate(1, 0),  new CellCoordinate(1, 1)
        });
    }

    [Fact]
    public void GetNeighbors_ShouldNotIncludeSelf()
    {
        // Arrange
        var coordinate = new CellCoordinate(5, 5);

        // Act
        var neighbors = coordinate.GetNeighbors().ToList();

        // Assert
        neighbors.Should().NotContain(coordinate);
    }

    [Fact]
    public void Equality_WithSameCoordinates_ShouldBeEqual()
    {
        // Arrange
        var coord1 = new CellCoordinate(5, 10);
        var coord2 = new CellCoordinate(5, 10);

        // Act & Assert
        coord1.Should().Be(coord2);
        (coord1 == coord2).Should().BeTrue();
        coord1.GetHashCode().Should().Be(coord2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentCoordinates_ShouldNotBeEqual()
    {
        // Arrange
        var coord1 = new CellCoordinate(5, 10);
        var coord2 = new CellCoordinate(10, 5);

        // Act & Assert
        coord1.Should().NotBe(coord2);
        (coord1 != coord2).Should().BeTrue();
    }
}