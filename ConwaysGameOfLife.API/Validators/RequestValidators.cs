using ConwaysGameOfLife.API.Models;
using ConwaysGameOfLife.Domain.Configuration;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ConwaysGameOfLife.API.Validators;

public class UploadBoardRequestValidator : AbstractValidator<UploadBoardRequest>
{
    private readonly GameOfLifeSettings _settings;

    public UploadBoardRequestValidator(IOptions<GameOfLifeSettings> settings)
    {
        _settings = settings.Value;
        
        RuleFor(x => x.AliveCells)
            .NotNull()
            .WithMessage("Alive cells cannot be null");

        RuleFor(x => x.MaxDimension)
            .GreaterThan(0)
            .When(x => x.MaxDimension.HasValue)
            .WithMessage("Max dimension must be greater than 0")
            .LessThanOrEqualTo(10000)
            .When(x => x.MaxDimension.HasValue)
            .WithMessage("Max dimension cannot exceed 10,000 for performance reasons");

        RuleForEach(x => x.AliveCells)
            .Must((request, cell) => IsValidCoordinate(cell, request.MaxDimension ?? _settings.DefaultMaxDimension))
            .WithMessage("All cell coordinates must be within the board boundaries (0 to MaxDimension-1)");

        RuleFor(x => x.AliveCells)
            .Must(cells => cells == null || cells.Count() <= 100000)
            .WithMessage("Cannot have more than 100,000 alive cells for performance reasons");
    }

    private static bool IsValidCoordinate(CellCoordinateDto cell, int maxDimension)
    {
        return cell.X >= 0 && cell.X < maxDimension && 
               cell.Y >= 0 && cell.Y < maxDimension;
    }
}

public class GetNStatesAheadRequestValidator : AbstractValidator<GetNStatesAheadRequest>
{
    public GetNStatesAheadRequestValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty()
            .WithMessage("Board ID cannot be empty");

        RuleFor(x => x.Generations)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Generations must be non-negative")
            .LessThanOrEqualTo(10000)
            .WithMessage("Cannot generate more than 10,000 generations at once for performance reasons");
    }
}

public class GetFinalStateRequestValidator : AbstractValidator<GetFinalStateRequest>
{
    private readonly GameOfLifeSettings _settings;

    public GetFinalStateRequestValidator(IOptions<GameOfLifeSettings> settings)
    {
        _settings = settings.Value;
        
        RuleFor(x => x.BoardId)
            .NotEmpty()
            .WithMessage("Board ID cannot be empty");

        RuleFor(x => x.MaxIterations)
            .GreaterThan(0)
            .When(x => x.MaxIterations.HasValue)
            .WithMessage("Max iterations must be greater than 0")
            .LessThanOrEqualTo(100000)
            .When(x => x.MaxIterations.HasValue)
            .WithMessage("Max iterations cannot exceed 100,000 for performance reasons");

        RuleFor(x => x.StableStateThreshold)
            .GreaterThan(0)
            .When(x => x.StableStateThreshold.HasValue)
            .WithMessage("Stable state threshold must be greater than 0")
            .LessThanOrEqualTo(1000)
            .When(x => x.StableStateThreshold.HasValue)
            .WithMessage("Stable state threshold cannot exceed 1,000");
    }
}