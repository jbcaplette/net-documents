namespace ConwaysGameOfLife.Domain.Configuration;

public class GameOfLifeSettings
{
    public const string SectionName = "GameOfLife";
    
    public int DefaultMaxDimension { get; set; } = 1000;
    public int DefaultMaxIterations { get; set; } = 1000;
    public int DefaultStableStateThreshold { get; set; } = 20;
    public int ProgressLoggingInterval { get; set; } = 100;
    public int MaxCycleDetectionLength { get; set; } = 10;
    public int CycleStabilityRequirement { get; set; } = 3;
}