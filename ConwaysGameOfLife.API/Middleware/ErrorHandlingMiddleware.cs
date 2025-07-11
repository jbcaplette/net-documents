using ConwaysGameOfLife.API.Models;
using FluentValidation;

namespace ConwaysGameOfLife.API.Middleware;

public static class ErrorHandling
{
    public static IResult HandleError(Exception ex)
    {
        var errorResponse = new ErrorResponse
        {
            Message = ex.Message,
            ErrorCode = ex.GetType().Name,
            Timestamp = DateTime.UtcNow
        };

        return ex switch
        {
            ArgumentException => Results.BadRequest(errorResponse),
            InvalidOperationException => Results.NotFound(errorResponse),
            _ => Results.Problem(detail: ex.Message, title: "An error occurred")
        };
    }

    public static async Task<IResult?> ValidateRequest<T>(T request, IValidator<T> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errorResponse = new ErrorResponse
            {
                Message = "Validation failed",
                ErrorCode = "ValidationError",
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage),
                Timestamp = DateTime.UtcNow
            };
            return Results.BadRequest(errorResponse);
        }
        return null;
    }
}