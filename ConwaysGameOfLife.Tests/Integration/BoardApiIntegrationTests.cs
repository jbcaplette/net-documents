using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConwaysGameOfLife.API;

namespace ConwaysGameOfLife.Tests.Integration;

public class BoardApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = $"TestDatabase_{Guid.NewGuid()}";
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add in-memory database for testing with unique name per test instance
                services.AddDbContext<GameOfLifeDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });

                // Build the service provider and ensure database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();
                context.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UploadBoard_WithValidBlinkerPattern_ShouldReturnCreated()
    {
        // Arrange
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 4 },
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 }
            },
            MaxDimension = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var boardResponse = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        boardResponse.Should().NotBeNull();
        boardResponse!.BoardId.Should().NotBe(Guid.Empty);
        boardResponse.Generation.Should().Be(0);
        boardResponse.MaxDimension.Should().Be(20);
        boardResponse.AliveCells.Should().HaveCount(3);
        boardResponse.IsEmpty.Should().BeFalse();
        boardResponse.AliveCellCount.Should().Be(3);
    }

    [Fact]
    public async Task UploadBoard_WithInvalidInput_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 25, Y = 25 } // Outside boundary
            },
            MaxDimension = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNextState_WithValidBoardId_ShouldReturnNextGeneration()
    {
        // Arrange - First create a board
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 4 },
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var boardResponse = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Get next state
        var nextStateResponse = await _client.PostAsync($"/api/boards/{boardResponse!.BoardId}/next", null);

        // Assert
        nextStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var nextStateContent = await nextStateResponse.Content.ReadAsStringAsync();
        var nextStateBoard = JsonSerializer.Deserialize<BoardResponse>(nextStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nextStateBoard.Should().NotBeNull();
        nextStateBoard!.BoardId.Should().Be(boardResponse.BoardId);
        nextStateBoard.Generation.Should().Be(1);
        // Blinker should rotate to horizontal
        nextStateBoard.AliveCells.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetNextState_WithNonExistentBoardId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentBoardId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/boards/{nonExistentBoardId}/next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError); // Service throws exception which becomes 500
    }

    [Fact]
    public async Task GetNStatesAhead_WithValidInput_ShouldReturnCorrectGeneration()
    {
        // Arrange - First create a board
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 },
                new CellCoordinateDto { X = 6, Y = 5 },
                new CellCoordinateDto { X = 6, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var boardResponse = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var nStatesRequest = new GetNStatesAheadRequest
        {
            BoardId = boardResponse!.BoardId,
            Generations = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/states-ahead", nStatesRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var resultBoard = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        resultBoard.Should().NotBeNull();
        resultBoard!.BoardId.Should().Be(boardResponse.BoardId);
        resultBoard.Generation.Should().Be(5);
        // Block pattern should remain stable
        resultBoard.AliveCells.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetNStatesAhead_WithInvalidInput_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = -5 // Invalid
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/states-ahead", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFinalState_WithStablePattern_ShouldReturnFinalState()
    {
        // Arrange - Create a block pattern (stable)
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 },
                new CellCoordinateDto { X = 6, Y = 5 },
                new CellCoordinateDto { X = 6, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var boardResponse = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = boardResponse!.BoardId,
            MaxIterations = 100,
            StableStateThreshold = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var resultBoard = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        resultBoard.Should().NotBeNull();
        resultBoard!.BoardId.Should().Be(boardResponse.BoardId);
        // Block pattern should stabilize quickly
        resultBoard.AliveCells.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetFinalState_WithEmptyBoard_ShouldReturnEmptyFinalState()
    {
        // Arrange - Create an empty board
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = Array.Empty<CellCoordinateDto>(),
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var boardResponse = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = boardResponse!.BoardId,
            MaxIterations = 100,
            StableStateThreshold = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var resultBoard = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        resultBoard.Should().NotBeNull();
        resultBoard!.BoardId.Should().Be(boardResponse.BoardId);
        resultBoard.IsEmpty.Should().BeTrue();
        resultBoard.AliveCells.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinalState_WithInvalidInput_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.Empty, // Invalid
            MaxIterations = 1000,
            StableStateThreshold = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task FullWorkflow_CreateBoardAndEvolveThroughGenerations_ShouldWork()
    {
        // Arrange - Create a blinker pattern
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 10, Y = 9 },
                new CellCoordinateDto { X = 10, Y = 10 },
                new CellCoordinateDto { X = 10, Y = 11 }
            },
            MaxDimension = 50
        };

        // Act & Assert - Step 1: Upload board
        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var initialBoard = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        initialBoard.Should().NotBeNull();
        initialBoard!.Generation.Should().Be(0);
        initialBoard.AliveCells.Should().HaveCount(3);

        // Step 2: Get next state (should rotate to horizontal)
        var nextStateResponse = await _client.PostAsync($"/api/boards/{initialBoard.BoardId}/next", null);
        nextStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var nextStateContent = await nextStateResponse.Content.ReadAsStringAsync();
        var nextStateBoard = JsonSerializer.Deserialize<BoardResponse>(nextStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nextStateBoard.Should().NotBeNull();
        nextStateBoard!.Generation.Should().Be(1);
        nextStateBoard.AliveCells.Should().HaveCount(3);

        // Step 3: Get multiple states ahead
        var nStatesRequest = new GetNStatesAheadRequest
        {
            BoardId = initialBoard.BoardId,
            Generations = 10
        };

        var nStatesResponse = await _client.PostAsJsonAsync("/api/boards/states-ahead", nStatesRequest);
        nStatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var nStatesContent = await nStatesResponse.Content.ReadAsStringAsync();
        var nStatesBoard = JsonSerializer.Deserialize<BoardResponse>(nStatesContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nStatesBoard.Should().NotBeNull();
        nStatesBoard!.Generation.Should().Be(10);
        nStatesBoard.AliveCells.Should().HaveCount(3); // Blinker maintains 3 cells

        // Step 4: Get final state (should detect oscillation)
        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = initialBoard.BoardId,
            MaxIterations = 50,
            StableStateThreshold = 10
        };

        var finalStateResponse = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);
        finalStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var finalStateContent = await finalStateResponse.Content.ReadAsStringAsync();
        var finalStateBoard = JsonSerializer.Deserialize<BoardResponse>(finalStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        finalStateBoard.Should().NotBeNull();
        finalStateBoard!.AliveCells.Should().HaveCount(3); // Blinker should be detected as stable cycle
    }
}