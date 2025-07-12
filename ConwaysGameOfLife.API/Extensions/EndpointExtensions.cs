using ConwaysGameOfLife.API.Mappers;
using ConwaysGameOfLife.API.Middleware;
using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Domain.Configuration;
using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
            ILogger<IBoardService> logger,
            IOptions<GameOfLifeSettings> gameSettings) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            var settings = gameSettings.Value;
            
            try
            {
                var maxDimension = request.MaxDimension ?? settings.DefaultMaxDimension;
                
                logger.LogInformation("Creating new board with {AliveCellCount} alive cells and max dimension {MaxDimension}. CorrelationId: {CorrelationId}", 
                    request.AliveCells.Count(), maxDimension, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var coordinates = request.AliveCells.ToCoordinates();
                var board = await boardService.CreateBoardAsync(coordinates, maxDimension);
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
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // 2. Get Next State
        boardGroup.MapPost("{boardId:guid}/next", async (
            Guid boardId,
            IBoardService boardService,
            IValidator<GetNextStateRequest> validator,
            ILogger<IBoardService> logger) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                var request = new GetNextStateRequest { BoardId = boardId };
                
                logger.LogInformation("Getting next state for board {BoardId}. CorrelationId: {CorrelationId}", 
                    boardId, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

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
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

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
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // 4. Get Final State
        boardGroup.MapPost("final-state", async (
            [FromBody] GetFinalStateRequest request,
            IBoardService boardService,
            IValidator<GetFinalStateRequest> validator,
            ILogger<IBoardService> logger,
            IOptions<GameOfLifeSettings> gameSettings) =>
        {
            var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString();
            var settings = gameSettings.Value;
            
            try
            {
                var maxIterations = request.MaxIterations ?? settings.DefaultMaxIterations;
                var stableThreshold = request.StableStateThreshold ?? settings.DefaultStableStateThreshold;
                
                logger.LogInformation("Computing final state for board {BoardId} with max iterations {MaxIterations} and stable threshold {StableThreshold}. CorrelationId: {CorrelationId}", 
                    request.BoardId, maxIterations, stableThreshold, correlationId);

                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var board = await boardService.GetFinalStateAsync(
                    new BoardId(request.BoardId),
                    maxIterations,
                    stableThreshold);
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
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}