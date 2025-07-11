namespace ConwaysGameOfLife.Domain.ValueObjects;

public record BoardId
{
    public Guid Value { get; init; }

    public BoardId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Board ID cannot be empty", nameof(value));
        
        Value = value;
    }

    public static BoardId NewId() => new(Guid.NewGuid());

    public static implicit operator Guid(BoardId boardId) => boardId.Value;
    public static implicit operator BoardId(Guid value) => new(value);
}