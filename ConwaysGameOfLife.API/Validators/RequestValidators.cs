using ConwaysGameOfLife.API.Models;
using FluentValidation;

namespace ConwaysGameOfLife.API.Validators;

public class UploadBoardRequestValidator : AbstractValidator<UploadBoardRequest>
{
    public UploadBoardRequestValidator()
    {
        RuleFor(x => x.AliveCells)
            .NotNull()
            .WithMessage("Alive cells cannot be null");

        RuleFor(x => x.MaxDimension)
            .GreaterThan(0)
            .WithMessage("Max dimension must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Max dimension cannot exceed 10,000 for performance reasons");

        RuleForEach(x => x.AliveCells)
            .Must((request, cell) => IsValidCoordinate(cell, request.MaxDimension))
            .WithMessage("All cell coordinates must be within the board boundaries (0 to MaxDimension-1)");

        RuleFor(x => x.AliveCells)
            .Must(cells => cells.Count() <= 100000)
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
    public GetFinalStateRequestValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty()
            .WithMessage("Board ID cannot be empty");

        RuleFor(x => x.MaxIterations)
            .GreaterThan(0)
            .WithMessage("Max iterations must be greater than 0")
            .LessThanOrEqualTo(100000)
            .WithMessage("Max iterations cannot exceed 100,000 for performance reasons");

        RuleFor(x => x.StableStateThreshold)
            .GreaterThan(0)
            .WithMessage("Stable state threshold must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Stable state threshold cannot exceed 1,000");
    }
}