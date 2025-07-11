using ConwaysGameOfLife.API.Mappers;
using ConwaysGameOfLife.API.Middleware;
using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.API.Validators;
using ConwaysGameOfLife.Domain.Services;
using ConwaysGameOfLife.Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

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
            IValidator<UploadBoardRequest> validator) =>
        {
            try
            {
                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var coordinates = request.AliveCells.ToCoordinates();
                var board = await boardService.CreateBoardAsync(coordinates, request.MaxDimension);
                var response = board.ToResponse();

                return Results.Created($"/api/boards/{response.BoardId}", response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex);
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
            IBoardService boardService) =>
        {
            try
            {
                var board = await boardService.GetNextStateAsync(new BoardId(boardId));
                var response = board.ToResponse();
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex);
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
            IValidator<GetNStatesAheadRequest> validator) =>
        {
            try
            {
                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var board = await boardService.GetStateAfterGenerationsAsync(
                    new BoardId(request.BoardId), 
                    request.Generations);
                var response = board.ToResponse();
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex);
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
            IValidator<GetFinalStateRequest> validator) =>
        {
            try
            {
                var validationError = await ErrorHandling.ValidateRequest(request, validator);
                if (validationError != null) return validationError;

                var board = await boardService.GetFinalStateAsync(
                    new BoardId(request.BoardId),
                    request.MaxIterations,
                    request.StableStateThreshold);
                var response = board.ToResponse();
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return ErrorHandling.HandleError(ex);
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
        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck")
        .WithOpenApi();
    }
}