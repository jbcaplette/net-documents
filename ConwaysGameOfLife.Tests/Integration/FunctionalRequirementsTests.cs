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

/// <summary>
/// Comprehensive tests to validate all functional requirements from the original specification
/// </summary>
public class FunctionalRequirementsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public FunctionalRequirementsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add in-memory database for testing
                services.AddDbContext<GameOfLifeDbContext>(options =>
                {
                    options.UseInMemoryDatabase("FunctionalTestDatabase");
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
    public async Task Requirement1_UploadBoardState_ShouldAcceptNewBoardStateAndReturnUniqueId()
    {
        // Arrange
        var boardState = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 2, Y = 2 },
                new CellCoordinateDto { X = 3, Y = 3 }
            },
            MaxDimension = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", boardState);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.BoardId.Should().NotBe(Guid.Empty);
        result.AliveCells.Should().HaveCount(3);
        result.Generation.Should().Be(0);
        result.MaxDimension.Should().Be(100);
        
        // Verify response contains Location header with board ID
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(result.BoardId.ToString());
    }

    [Fact]
    public async Task Requirement2_GetNextState_ShouldReturnNextGenerationGivenBoardId()
    {
        // Arrange - First upload a board
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 4 }, // Blinker pattern
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Get next state
        var nextStateResponse = await _client.PostAsync($"/api/boards/{board!.BoardId}/next", null);

        // Assert
        nextStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var nextStateContent = await nextStateResponse.Content.ReadAsStringAsync();
        var nextState = JsonSerializer.Deserialize<BoardResponse>(nextStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nextState.Should().NotBeNull();
        nextState!.BoardId.Should().Be(board.BoardId);
        nextState.Generation.Should().Be(1);
        nextState.AliveCells.Should().HaveCount(3); // Blinker maintains 3 cells
        
        // Verify the blinker has rotated (vertical to horizontal)
        var expectedHorizontalBlinker = new[]
        {
            new { X = 4, Y = 5 },
            new { X = 5, Y = 5 },
            new { X = 6, Y = 5 }
        };
        
        nextState.AliveCells.Should().BeEquivalentTo(expectedHorizontalBlinker);
    }

    [Fact]
    public async Task Requirement3_GetNStatesAhead_ShouldReturnBoardStateAfterNGenerations()
    {
        // Arrange - Upload a stable block pattern
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 5 }, // Block pattern (stable)
                new CellCoordinateDto { X = 5, Y = 6 },
                new CellCoordinateDto { X = 6, Y = 5 },
                new CellCoordinateDto { X = 6, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var nStatesRequest = new GetNStatesAheadRequest
        {
            BoardId = board!.BoardId,
            Generations = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/states-ahead", nStatesRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.BoardId.Should().Be(board.BoardId);
        result.Generation.Should().Be(10);
        result.AliveCells.Should().HaveCount(4); // Block pattern remains stable
        
        // Verify block pattern is unchanged (stable)
        result.AliveCells.Should().BeEquivalentTo(board.AliveCells);
    }

    [Fact]
    public async Task Requirement4_GetFinalState_ShouldReturnFinalStableStateWhenReached()
    {
        // Arrange - Upload a pattern that quickly stabilizes (block)
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 10, Y = 10 }, // Block pattern
                new CellCoordinateDto { X = 10, Y = 11 },
                new CellCoordinateDto { X = 11, Y = 10 },
                new CellCoordinateDto { X = 11, Y = 11 }
            },
            MaxDimension = 50
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = board!.BoardId,
            MaxIterations = 100,
            StableStateThreshold = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.BoardId.Should().Be(board.BoardId);
        result.AliveCells.Should().HaveCount(4); // Block pattern is stable
        result.AliveCells.Should().BeEquivalentTo(board.AliveCells); // Should be unchanged
    }

    [Fact]
    public async Task Requirement4_GetFinalState_ShouldDetectOscillatingPattern()
    {
        // Arrange - Upload a blinker pattern (oscillates with period 2)
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 10, Y = 9 }, // Blinker pattern
                new CellCoordinateDto { X = 10, Y = 10 },
                new CellCoordinateDto { X = 10, Y = 11 }
            },
            MaxDimension = 50
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = board!.BoardId,
            MaxIterations = 100,
            StableStateThreshold = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.BoardId.Should().Be(board.BoardId);
        result.AliveCells.Should().HaveCount(3); // Blinker maintains 3 cells
        result.Generation.Should().BeGreaterThan(0); // Should have evolved
    }

    [Fact]
    public async Task Requirement4_GetFinalState_ShouldReturnNotFoundWhenNotStableWithinLimit()
    {
        // Arrange - Upload a complex pattern that may not stabilize quickly
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                // Create a more complex pattern that might not stabilize quickly
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 1, Y = 2 },
                new CellCoordinateDto { X = 2, Y = 1 },
                new CellCoordinateDto { X = 10, Y = 10 },
                new CellCoordinateDto { X = 10, Y = 11 },
                new CellCoordinateDto { X = 11, Y = 10 },
                new CellCoordinateDto { X = 20, Y = 20 },
                new CellCoordinateDto { X = 21, Y = 20 },
                new CellCoordinateDto { X = 20, Y = 21 },
                new CellCoordinateDto { X = 20, Y = 22 },
                new CellCoordinateDto { X = 20, Y = 23 }
            },
            MaxDimension = 200
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = board!.BoardId,
            MaxIterations = 5, // Very low limit
            StableStateThreshold = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);

        // Assert - Should return not found when not stable within limit
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NonFunctionalRequirement_Persistence_BoardStatesShouldPersistBetweenRequests()
    {
        // Arrange - Upload a board
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 1, Y = 2 },
                new CellCoordinateDto { X = 2, Y = 1 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act 1 - Get next state
        var nextStateResponse1 = await _client.PostAsync($"/api/boards/{board!.BoardId}/next", null);
        var nextStateContent1 = await nextStateResponse1.Content.ReadAsStringAsync();
        var nextState1 = JsonSerializer.Deserialize<BoardResponse>(nextStateContent1, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act 2 - Get next state again (should continue from previous state)
        var nextStateResponse2 = await _client.PostAsync($"/api/boards/{board.BoardId}/next", null);
        var nextStateContent2 = await nextStateResponse2.Content.ReadAsStringAsync();
        var nextState2 = JsonSerializer.Deserialize<BoardResponse>(nextStateContent2, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert - States should be persisted and continue evolving
        nextState1!.Generation.Should().Be(1);
        nextState2!.Generation.Should().Be(2);
        nextState1.BoardId.Should().Be(board.BoardId);
        nextState2.BoardId.Should().Be(board.BoardId);
    }

    [Fact]
    public async Task NonFunctionalRequirement_ErrorHandling_ShouldProvideAppropriateErrorMessages()
    {
        // Test various error scenarios

        // 1. Invalid board ID
        var invalidBoardId = Guid.NewGuid();
        var nextStateResponse = await _client.PostAsync($"/api/boards/{invalidBoardId}/next", null);
        nextStateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Invalid validation
        var invalidRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 100, Y = 100 } // Outside boundary
            },
            MaxDimension = 50
        };
        var invalidResponse = await _client.PostAsJsonAsync("/api/boards", invalidRequest);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3. Negative generations
        var negativeGenRequest = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = -5
        };
        var negativeGenResponse = await _client.PostAsJsonAsync("/api/boards/states-ahead", negativeGenRequest);
        negativeGenResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullWorkflow_AllRequirements_EndToEndScenario()
    {
        // This test validates the complete workflow covering all functional requirements

        // Step 1: Upload Board State (Requirement 1)
        var uploadRequest = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 5, Y = 4 }, // Blinker pattern
                new CellCoordinateDto { X = 5, Y = 5 },
                new CellCoordinateDto { X = 5, Y = 6 }
            },
            MaxDimension = 20
        };

        var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardResponse>(uploadContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        board.Should().NotBeNull();
        board!.BoardId.Should().NotBe(Guid.Empty);
        board.Generation.Should().Be(0);

        // Step 2: Get Next State (Requirement 2)
        var nextStateResponse = await _client.PostAsync($"/api/boards/{board.BoardId}/next", null);
        nextStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var nextStateContent = await nextStateResponse.Content.ReadAsStringAsync();
        var nextState = JsonSerializer.Deserialize<BoardResponse>(nextStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nextState!.Generation.Should().Be(1);

        // Step 3: Get N States Ahead (Requirement 3)
        var nStatesRequest = new GetNStatesAheadRequest
        {
            BoardId = board.BoardId,
            Generations = 5
        };

        var nStatesResponse = await _client.PostAsJsonAsync("/api/boards/states-ahead", nStatesRequest);
        nStatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var nStatesContent = await nStatesResponse.Content.ReadAsStringAsync();
        var nStatesResult = JsonSerializer.Deserialize<BoardResponse>(nStatesContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        nStatesResult!.Generation.Should().Be(6);

        // Step 4: Get Final State (Requirement 4)
        var finalStateRequest = new GetFinalStateRequest
        {
            BoardId = board.BoardId,
            MaxIterations = 50,
            StableStateThreshold = 10
        };

        var finalStateResponse = await _client.PostAsJsonAsync("/api/boards/final-state", finalStateRequest);
        finalStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalStateContent = await finalStateResponse.Content.ReadAsStringAsync();
        var finalState = JsonSerializer.Deserialize<BoardResponse>(finalStateContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        finalState!.BoardId.Should().Be(board.BoardId);
        finalState.AliveCells.Should().HaveCount(3); // Blinker pattern should be detected as stable cycle

        // Verify persistence - all operations used the same board ID
        board.BoardId.Should().Be(nextState.BoardId);
        board.BoardId.Should().Be(nStatesResult.BoardId);
        board.BoardId.Should().Be(finalState.BoardId);
    }
}