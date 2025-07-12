using ConwaysGameOfLife.API.Models;
using FluentValidation;

namespace ConwaysGameOfLife.API.Middleware;

public static class ErrorHandling
{
    public static IResult HandleError(Exception ex, ILogger? logger = null)
    {
        // Log the exception with correlation ID
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        logger?.LogError(ex, "Error occurred while processing request. CorrelationId: {CorrelationId}", correlationId);

        var errorResponse = new ErrorResponse
        {
            Message = ex.Message,
            ErrorCode = ex.GetType().Name,
            Timestamp = DateTime.UtcNow
        };

        return ex switch
        {
            ArgumentException argEx => LogAndReturnBadRequest(argEx, errorResponse, logger, correlationId),
            InvalidOperationException invOpEx => LogAndReturnNotFound(invOpEx, errorResponse, logger, correlationId),
            _ => LogAndReturnInternalServerError(ex, errorResponse, logger, correlationId)
        };
    }

    private static IResult LogAndReturnBadRequest(ArgumentException argEx, ErrorResponse errorResponse, ILogger? logger, string correlationId)
    {
        logger?.LogWarning("Argument exception: {Message}. CorrelationId: {CorrelationId}", argEx.Message, correlationId);
        return Results.BadRequest(errorResponse);
    }

    private static IResult LogAndReturnNotFound(InvalidOperationException invOpEx, ErrorResponse errorResponse, ILogger? logger, string correlationId)
    {
        logger?.LogWarning("Invalid operation: {Message}. CorrelationId: {CorrelationId}", invOpEx.Message, correlationId);
        return Results.NotFound(errorResponse);
    }

    private static IResult LogAndReturnInternalServerError(Exception ex, ErrorResponse errorResponse, ILogger? logger, string correlationId)
    {
        logger?.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
        return Results.Json(errorResponse, statusCode: 500);
    }

    public static async Task<IResult?> ValidateRequest<T>(T request, IValidator<T> validator, ILogger? logger = null)
    {
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
        
        logger?.LogDebug("Validating request of type {RequestType}. CorrelationId: {CorrelationId}", typeof(T).Name, correlationId);
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            
            logger?.LogWarning("Validation failed for {RequestType}. Errors: {@ValidationErrors}. CorrelationId: {CorrelationId}", 
                typeof(T).Name, validationErrors, correlationId);
            
            var errorResponse = new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "ValidationError",
                ValidationErrors = validationErrors,
                Timestamp = DateTime.UtcNow
            };
            return Results.BadRequest(errorResponse);
        }

        logger?.LogDebug("Validation passed for {RequestType}. CorrelationId: {CorrelationId}", typeof(T).Name, correlationId);
        return null;
    }
}