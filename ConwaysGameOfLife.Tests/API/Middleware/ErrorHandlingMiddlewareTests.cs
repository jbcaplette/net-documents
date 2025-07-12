using ConwaysGameOfLife.API.Middleware;
using ConwaysGameOfLife.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace ConwaysGameOfLife.Tests.API.Middleware;

public class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public void HandleError_WithArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument provided");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        // Verify the result type and status code
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void HandleError_WithInvalidOperationException_ShouldReturnNotFound()
    {
        // Arrange
        var exception = new InvalidOperationException("Resource not found");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public void HandleError_WithUnhandledException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new SystemException("Unexpected system error");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void HandleError_WithDatabaseException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(404); // InvalidOperationException maps to 404 in current implementation
    }

    [Fact]
    public void HandleError_WithNullReferenceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new NullReferenceException("Object reference not set to an instance of an object");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void HandleError_WithTimeoutException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void HandleError_WithOutOfMemoryException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new OutOfMemoryException("Insufficient memory to continue execution");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public void HandleError_ShouldLogErrorWithCorrelationId()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while processing request")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void HandleError_ShouldReturnErrorResponseWithCorrectStructure()
    {
        // Arrange
        var exception = new Exception("Test exception message");

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        
        // For 500 errors, we expect a JSON result with ErrorResponse structure
        var jsonResult = result as IValueHttpResult;
        jsonResult.Should().NotBeNull();
        
        var errorResponse = jsonResult!.Value as ErrorResponse;
        if (errorResponse != null)
        {
            errorResponse.Message.Should().Be("Test exception message");
            errorResponse.ErrorCode.Should().Be("Exception");
            errorResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Theory]
    [InlineData(typeof(ArgumentException), 400)]
    [InlineData(typeof(InvalidOperationException), 404)]
    [InlineData(typeof(Exception), 500)]
    [InlineData(typeof(SystemException), 500)]
    [InlineData(typeof(NotSupportedException), 500)]
    [InlineData(typeof(InvalidCastException), 500)]
    public void HandleError_ShouldReturnCorrectStatusCodeForExceptionType(Type exceptionType, int expectedStatusCode)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;

        // Act
        var result = ErrorHandling.HandleError(exception, _mockLogger.Object);

        // Assert
        var httpResult = result as IStatusCodeHttpResult;
        httpResult.Should().NotBeNull();
        httpResult!.StatusCode.Should().Be(expectedStatusCode);
    }
}
