using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace ConwaysGameOfLife.Tests.API.Validators;

public class RequestValidatorsTests
{
    private readonly Mock<IOptions<GameOfLifeSettings>> _mockSettings;
    
    public RequestValidatorsTests()
    {
        _mockSettings = new Mock<IOptions<GameOfLifeSettings>>();
        _mockSettings.Setup(x => x.Value).Returns(new GameOfLifeSettings
        {
            DefaultMaxDimension = 1000,
            DefaultMaxIterations = 1000,
            DefaultStableStateThreshold = 20,
            ProgressLoggingInterval = 100,
            MaxCycleDetectionLength = 10,
            CycleStabilityRequirement = 3
        });
    }

    [Fact]
    public void UploadBoardRequestValidator_WithValidRequest_ShouldPassValidation()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 1, Y = 2 },
                new CellCoordinateDto { X = 2, Y = 1 }
            },
            MaxDimension = 100
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UploadBoardRequestValidator_WithNullAliveCells_ShouldFailValidation()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = null!,
            MaxDimension = 100
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Alive cells cannot be null"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UploadBoardRequestValidator_WithInvalidMaxDimension_ShouldFailValidation(int invalidMaxDimension)
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = new[] { new CellCoordinateDto { X = 1, Y = 1 } },
            MaxDimension = invalidMaxDimension
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Max dimension must be greater than 0"));
    }

    [Fact]
    public void UploadBoardRequestValidator_WithTooLargeMaxDimension_ShouldFailValidation()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = new[] { new CellCoordinateDto { X = 1, Y = 1 } },
            MaxDimension = 20000 // Exceeds limit of 10,000
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Max dimension cannot exceed 10,000"));
    }

    [Fact]
    public void UploadBoardRequestValidator_WithCellsOutsideBoundary_ShouldFailValidation()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 10, Y = 10 } // Outside boundary (max is 9)
            },
            MaxDimension = 10
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("All cell coordinates must be within the board boundaries"));
    }

    [Fact]
    public void UploadBoardRequestValidator_WithTooManyAliveCells_ShouldFailValidation()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var aliveCells = Enumerable.Range(0, 100001) // Exceeds limit of 100,000
            .Select(i => new CellCoordinateDto { X = i % 1000, Y = i / 1000 });
        
        var request = new UploadBoardRequest
        {
            AliveCells = aliveCells,
            MaxDimension = 1000
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot have more than 100,000 alive cells"));
    }

    [Fact]
    public void UploadBoardRequestValidator_WithNullMaxDimension_ShouldUseDefaultFromSettings()
    {
        // Arrange
        var validator = new UploadBoardRequestValidator(_mockSettings.Object);
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 999, Y = 999 } // Should be valid with default dimension 1000
            },
            MaxDimension = null
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetNStatesAheadRequestValidator_WithValidRequest_ShouldPassValidation()
    {
        // Arrange
        var validator = new GetNStatesAheadRequestValidator();
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = 10
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetNStatesAheadRequestValidator_WithEmptyBoardId_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetNStatesAheadRequestValidator();
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.Empty,
            Generations = 10
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Board ID cannot be empty"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetNStatesAheadRequestValidator_WithNegativeGenerations_ShouldFailValidation(int negativeGenerations)
    {
        // Arrange
        var validator = new GetNStatesAheadRequestValidator();
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = negativeGenerations
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Generations must be non-negative"));
    }

    [Fact]
    public void GetNStatesAheadRequestValidator_WithTooManyGenerations_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetNStatesAheadRequestValidator();
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = 20000 // Exceeds limit of 10,000
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Cannot generate more than 10,000 generations"));
    }

    [Fact]
    public void GetFinalStateRequestValidator_WithValidRequest_ShouldPassValidation()
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = 1000,
            StableStateThreshold = 20
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetFinalStateRequestValidator_WithEmptyBoardId_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.Empty,
            MaxIterations = 1000,
            StableStateThreshold = 20
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Board ID cannot be empty"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetFinalStateRequestValidator_WithInvalidMaxIterations_ShouldFailValidation(int invalidMaxIterations)
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = invalidMaxIterations,
            StableStateThreshold = 20
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Max iterations must be greater than 0"));
    }

    [Fact]
    public void GetFinalStateRequestValidator_WithTooManyMaxIterations_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = 200000, // Exceeds limit of 100,000
            StableStateThreshold = 20
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Max iterations cannot exceed 100,000"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GetFinalStateRequestValidator_WithInvalidStableStateThreshold_ShouldFailValidation(int invalidThreshold)
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = 1000,
            StableStateThreshold = invalidThreshold
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Stable state threshold must be greater than 0"));
    }

    [Fact]
    public void GetFinalStateRequestValidator_WithTooLargeStableStateThreshold_ShouldFailValidation()
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = 1000,
            StableStateThreshold = 2000 // Exceeds limit of 1,000
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Stable state threshold cannot exceed 1,000"));
    }

    [Fact]
    public void GetFinalStateRequestValidator_WithNullValues_ShouldUseDefaultsFromSettings()
    {
        // Arrange
        var validator = new GetFinalStateRequestValidator(_mockSettings.Object);
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = null,
            StableStateThreshold = null
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue(); // Should pass validation using defaults
    }
}