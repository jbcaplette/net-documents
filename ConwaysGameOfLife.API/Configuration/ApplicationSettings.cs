namespace ConwaysGameOfLife.API.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string Provider { get; set; } = "SQLite";
    public int CommandTimeout { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public bool UseConnectionPooling { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}

public class ConnectionPoolingSettings
{
    public const string SectionName = "ConnectionPooling";
    
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 5;
    public int ConnectionLifetime { get; set; } = 300; // seconds
    public int ConnectionIdleTimeout { get; set; } = 30; // seconds
}

public class LoggingSettings
{
    public const string SectionName = "Serilog";
    
    public FileLoggingSettings File { get; set; } = new();
    public TelemetrySettings Telemetry { get; set; } = new();
}

public class FileLoggingSettings
{
    public string Path { get; set; } = "logs/conways-game-of-life-.txt";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 7;
    public string FileSizeLimitBytes { get; set; } = "10MB";
    public bool RollOnFileSizeLimit { get; set; } = true;
    public bool Buffered { get; set; } = true;
}

public class TelemetrySettings
{
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = true;
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseBody { get; set; } = false;
    public string[] ExcludedPaths { get; set; } = [];
}