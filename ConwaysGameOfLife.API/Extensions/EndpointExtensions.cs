using ConwaysGameOfLife.API.Mappers;
using ConwaysGameOfLife.API.Middleware;
using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ConwaysGameOfLife.API.Extensions;

public static class EndpointExtensions
{
    public static void MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var boardGroup = app.MapGroup("/api/boards");

        // 1. Upload Board State
        boardGroup.MapPost("", async (
            [FromBody] UploadBoardRequest request,
            IBoardService boardService,
            IValidator<UploadBoardRequest> validator,
            ILogger<IBoardService> logger) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                logger.LogInformation("Creating new board with {AliveCellCount} alive cells and max dimension {MaxDimension}. CorrelationId: {CorrelationId}", 
                    request.AliveCells.Count(), request.MaxDimension, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var coordinates = request.AliveCells.ToCoordinates();
                var board = await boardService.CreateBoardAsync(coordinates, request.MaxDimension);
                var response = board.ToResponse();

                logger.LogInformation("Successfully created board with ID {BoardId}. CorrelationId: {CorrelationId}", 
                    response.BoardId, correlationId);

                return Results.Created($"/api/boards/{response.BoardId}", response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex, logger);
            }
        })
        .WithName("UploadBoard")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Upload a new board state",
            Description = "Creates a new Conway's Game of Life board with the specified alive cells and returns a unique board ID."
        })
        .Produces<BoardResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // 2. Get Next State
        boardGroup.MapPost("{boardId:guid}/next", async (
            Guid boardId,
            IBoardService boardService,
            ILogger<IBoardService> logger) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                logger.LogInformation("Getting next state for board {BoardId}. CorrelationId: {CorrelationId}", 
                    boardId, correlationId);

                var board = await boardService.GetNextStateAsync(new BoardId(boardId));
                var response = board.ToResponse();

                logger.LogInformation("Successfully computed next state for board {BoardId}, generation {Generation}. CorrelationId: {CorrelationId}", 
                    boardId, response.Generation, correlationId);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex, logger);
            }
        })
        .WithName("GetNextState")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get the next generation state",
            Description = "Returns the board state after one generation given a board ID."
        })
        .Produces<BoardResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 3. Get N States Ahead
        boardGroup.MapPost("states-ahead", async (
            [FromBody] GetNStatesAheadRequest request,
            IBoardService boardService,
            IValidator<GetNStatesAheadRequest> validator,
            ILogger<IBoardService> logger) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                logger.LogInformation("Computing {Generations} generations ahead for board {BoardId}. CorrelationId: {CorrelationId}", 
                    request.Generations, request.BoardId, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var board = await boardService.GetStateAfterGenerationsAsync(
                    new BoardId(request.BoardId), 
                    request.Generations);
                var response = board.ToResponse();

                logger.LogInformation("Successfully computed {Generations} generations for board {BoardId}, final generation {FinalGeneration}. CorrelationId: {CorrelationId}", 
                    request.Generations, request.BoardId, response.Generation, correlationId);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex, logger);
            }
        })
        .WithName("GetNStatesAhead")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get board state after N generations",
            Description = "Returns the board state after a specified number of generations."
        })
        .Produces<BoardResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 4. Get Final State
        boardGroup.MapPost("final-state", async (
            [FromBody] GetFinalStateRequest request,
            IBoardService boardService,
            IValidator<GetFinalStateRequest> validator,
            ILogger<IBoardService> logger) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                logger.LogInformation("Computing final state for board {BoardId} with max iterations {MaxIterations} and stable threshold {StableThreshold}. CorrelationId: {CorrelationId}", 
                    request.BoardId, request.MaxIterations, request.StableStateThreshold, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var board = await boardService.GetFinalStateAsync(
                    new BoardId(request.BoardId),
                    request.MaxIterations,
                    request.StableStateThreshold);
                var response = board.ToResponse();

                logger.LogInformation("Successfully computed final state for board {BoardId}, final generation {FinalGeneration}. CorrelationId: {CorrelationId}", 
                    request.BoardId, response.Generation, correlationId);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex, logger);
            }
        })
        .WithName("GetFinalState")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Get the final stable state",
            Description = "Returns the final stable state of the board (when it no longer changes or reaches a stable cycle)."
        })
        .Produces<BoardResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Basic health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds,
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));
            }
        })
        .WithName("HealthCheck")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Health check endpoint",
            Description = "Returns the health status of the API and its dependencies including database connectivity."
        });

        // Detailed health check endpoint for monitoring tools
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                var response = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        })
        .WithName("ReadinessCheck")
        .WithOpenApi();

        // Liveness check endpoint (simple ping)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // Exclude all checks, just return healthy if the service is up
        })
        .WithName("LivenessCheck")
        .WithOpenApi();
    }
}