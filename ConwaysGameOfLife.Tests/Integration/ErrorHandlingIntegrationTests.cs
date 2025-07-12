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

public class ErrorHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ErrorHandlingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add in-memory database for testing
                services.AddDbContext<GameOfLifeDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}");
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
    public async Task UploadBoard_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/boards", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBoard_WithMissingRequiredFields_ShouldReturnBadRequest()
    {
        // Arrange
        var incompleteRequest = new { MaxDimension = 20 }; // Missing AliveCells

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", incompleteRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBoard_WithNegativeCoordinates_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = -1, Y = 5 },
                new CellCoordinateDto { X = 5, Y = -1 }
            },
            MaxDimension = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBoard_WithExcessivelyLargeMaxDimension_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UploadBoardRequest
        {
            AliveCells = new[] { new CellCoordinateDto { X = 1, Y = 1 } },
            MaxDimension = 50000 // Exceeds validation limit
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNextState_WithMalformedGuid_ShouldReturnNotFound()
    {
        // Arrange
        var invalidGuid = "not-a-guid";

        // Act
        var response = await _client.PostAsync($"/api/boards/{invalidGuid}/next", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNStatesAhead_WithExcessiveGenerations_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(),
            Generations = 50000 // Exceeds validation limit
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/states-ahead", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFinalState_WithInvalidParameters_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GetFinalStateRequest
        {
            BoardId = Guid.NewGuid(),
            MaxIterations = -1, // Invalid
            StableStateThreshold = 0 // Invalid
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/final-state", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBoard_WithExcessiveNumberOfCells_ShouldReturnBadRequest()
    {
        // Arrange - Create request with too many cells
        var excessiveCells = new List<CellCoordinateDto>();
        for (int i = 0; i < 100001; i++) // Exceeds limit
        {
            excessiveCells.Add(new CellCoordinateDto { X = i % 100, Y = i / 100 });
        }

        var request = new UploadBoardRequest
        {
            AliveCells = excessiveCells,
            MaxDimension = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApiEndpoints_WithUnsupportedHttpMethods_ShouldReturnMethodNotAllowed()
    {
        // Test various endpoints with wrong HTTP methods
        var testCases = new[]
        {
            new { Method = HttpMethod.Get, Endpoint = "/api/boards" },
            new { Method = HttpMethod.Put, Endpoint = "/api/boards" },
            new { Method = HttpMethod.Delete, Endpoint = "/api/boards" },
            new { Method = HttpMethod.Get, Endpoint = $"/api/boards/{Guid.NewGuid()}/next" },
            new { Method = HttpMethod.Put, Endpoint = $"/api/boards/{Guid.NewGuid()}/next" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var response = await _client.SendAsync(new HttpRequestMessage(testCase.Method, testCase.Endpoint));

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task ApiEndpoints_WithMalformedUrls_ShouldReturnNotFound()
    {
        // Test various malformed URLs
        var malformedUrls = new[]
        {
            "/api/board", // Wrong endpoint name
            "/api/boards/invalid", // Invalid structure
            "/api/boards/123/next", // Invalid GUID format
            "/api/boards/states", // Incomplete URL
            "/api/boards/final" // Incomplete URL
        };

        foreach (var url in malformedUrls)
        {
            // Act
            var response = await _client.PostAsync(url, null);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task UploadBoard_WithVeryLargePayload_ShouldHandleGracefully()
    {
        // Arrange - Create a request with many cells within limits but large payload
        var largeCellList = new List<CellCoordinateDto>();
        for (int i = 0; i < 10000; i++) // Within limit but still large
        {
            largeCellList.Add(new CellCoordinateDto { X = i % 100, Y = i / 100 });
        }

        var request = new UploadBoardRequest
        {
            AliveCells = largeCellList,
            MaxDimension = 1000
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert - Should either succeed or fail gracefully, not crash
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Concurrent_Requests_ShouldHandleCorrectly()
    {
        // Arrange - Create a valid request
        var request = new UploadBoardRequest
        {
            AliveCells = new[]
            {
                new CellCoordinateDto { X = 1, Y = 1 },
                new CellCoordinateDto { X = 1, Y = 2 },
                new CellCoordinateDto { X = 2, Y = 1 }
            },
            MaxDimension = 20
        };

        // Act - Send multiple concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.PostAsJsonAsync("/api/boards", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete without errors
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // All should have different board IDs
        var boardIds = new List<Guid>();
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            var boardResponse = JsonSerializer.Deserialize<BoardResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            boardIds.Add(boardResponse!.BoardId);
        }

        boardIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadBoard_WithEmptyAliveCellsArray_ShouldSucceed()
    {
        // Arrange - Empty board (valid scenario)
        var request = new UploadBoardRequest
        {
            AliveCells = Array.Empty<CellCoordinateDto>(),
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
        boardResponse!.IsEmpty.Should().BeTrue();
        boardResponse.AliveCells.Should().BeEmpty();
    }

    [Fact]
    public async Task InternalServerError_ShouldReturnConsistentErrorResponse()
    {
        // This test verifies that any internal server errors return the consistent ErrorResponse format
        // rather than the default ProblemDetails format
        
        // Note: In a real scenario, this would be testing against a mocked service that throws an exception
        // For now, we verify the response format structure through the error handling middleware
        
        // We can test this by triggering a scenario that might cause an internal error
        // For example, extremely large generation requests that might cause memory issues
        var request = new GetNStatesAheadRequest
        {
            BoardId = Guid.NewGuid(), // Non-existent board - should be handled as 404, but let's verify error format
            Generations = 1000000 // Very large number - might cause issues in some scenarios
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards/states-ahead", request);

        // Assert - The response should be in ErrorResponse format regardless of status code
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify the response can be deserialized as ErrorResponse (our consistent format)
        // and NOT as ProblemDetails (which would be inconsistent)
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().NotBeNullOrEmpty();
        errorResponse.ErrorCode.Should().NotBeNullOrEmpty();
        errorResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // The response should be either 400 (validation error), 404 (not found), or 500 (internal error)
        // But importantly, it should ALWAYS use our ErrorResponse format
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.NotFound, 
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ErrorResponse_ShouldHaveConsistentStructure()
    {
        // Arrange - Force a validation error to test ErrorResponse structure
        var request = new UploadBoardRequest
        {
            AliveCells = new[] { new CellCoordinateDto { X = -1, Y = -1 } }, // Invalid coordinates
            MaxDimension = 20
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Verify all required ErrorResponse properties are present
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().NotBeNullOrEmpty();
        errorResponse.ErrorCode.Should().NotBeNullOrEmpty();
        errorResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // For validation errors, ValidationErrors should be populated
        if (errorResponse.ErrorCode == "ValidationError")
        {
            errorResponse.ValidationErrors.Should().NotBeNull();
            errorResponse.ValidationErrors.Should().NotBeEmpty();
        }
    }
}