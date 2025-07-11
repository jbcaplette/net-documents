using ConwaysGameOfLife.Domain.ValueObjects;
using FluentValidation;

namespace ConwaysGameOfLife.Domain.Validators;

public class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardRequestValidator()
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

    private static bool IsValidCoordinate(CellCoordinate cell, int maxDimension)
    {
        return cell.X >= 0 && cell.X < maxDimension && 
               cell.Y >= 0 && cell.Y < maxDimension;
    }
}

public record CreateBoardRequest(IEnumerable<CellCoordinate> AliveCells, int MaxDimension = 1000);