# Conway's Game of Life API

A production-ready RESTful API implementation of Conway's Game of Life built with .NET 8, designed for scalability, performance, and maintainability.

## Overview

This API provides a complete implementation of Conway's Game of Life cellular automaton with persistent board state management, generation evolution, and advanced pattern detection capabilities. The service is built following clean architecture principles and includes comprehensive testing, logging, and monitoring features.

## Features

### Core Functionality
- **Board State Management**: Upload and persist 2D cell grids with unique identifiers
- **Generation Evolution**: Compute next generation states following Conway's rules
- **Multi-Generation Simulation**: Calculate board states N generations ahead
- **Final State Detection**: Automatically detect stable states and oscillating patterns
- **Pattern Recognition**: Identify still lifes, oscillators, and stable cycles

### Production Features
- **Persistent Storage**: SQLite database with Entity Framework Core
- **Performance Optimization**: Efficient algorithms with configurable limits
- **Comprehensive Logging**: Structured logging with Serilog
- **Error Handling**: Robust error handling with detailed error responses
- **Input Validation**: FluentValidation for request validation
- **API Documentation**: OpenAPI/Swagger documentation
- **Configuration Management**: Environment-specific settings

## Quick Start

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd net-documents
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the API**
   ```bash
   dotnet run --project ConwaysGameOfLife.API
   ```

4. **Access Swagger UI** (Development only)
   ```
   https://localhost:7140/swagger
   ```
   *Note: You may need to accept the self-signed certificate warning in your browser*

## API Endpoints

### 1. Upload Board State
**POST** `/api/boards`

Creates a new board with specified alive cells.

```json
{
  "aliveCells": [
    {"x": 5, "y": 4},
    {"x": 5, "y": 5},
    {"x": 5, "y": 6}
  ],
  "maxDimension": 20
}
```

**Response**: Returns unique board ID and initial state
```json
{
  "boardId": "123e4567-e89b-12d3-a456-426614174000",
  "aliveCells": [...],
  "generation": 0,
  "maxDimension": 20,
  "createdAt": "2025-01-01T00:00:00Z",
  "isEmtpy": false,
  "aliveCellCount": 3
}
```

### 2. Get Next Generation
**POST** `/api/boards/{boardId}/next`

Returns the board state after one generation.

### 3. Get N Generations Ahead
**POST** `/api/boards/states-ahead`

```json
{
  "boardId": "123e4567-e89b-12d3-a456-426614174000",
  "generations": 10
}
```

Returns the board state after N generations.

### 4. Get Final State
**POST** `/api/boards/final-state`

```json
{
  "boardId": "123e4567-e89b-12d3-a456-426614174000",
  "maxIterations": 1000,
  "stableStateThreshold": 20
}
```

Returns the final stable state (still life or stable oscillator).

## Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
ConwaysGameOfLife.Domain/        # Core business logic
├── Entities/                   # Domain entities (Board, BoardHistory)
├── ValueObjects/               # Value objects (BoardId, CellCoordinate)
├── Services/                   # Domain services and interfaces
└── Configuration/              # Domain configuration

ConwaysGameOfLife.Infrastructure/ # Data persistence
├── Persistence/                # EF Core DbContext and repositories
├── Repositories/               # Repository implementations
└── Extensions/                 # Infrastructure extensions

ConwaysGameOfLife.API/          # API layer
├── Extensions/                 # API configuration extensions
├── Middleware/                 # Custom middleware
├── Models/                     # API request/response models
├── Mappers/                    # Domain to API model mapping
└── Validators/                 # Request validation

ConwaysGameOfLife.Tests/        # Comprehensive test suite
├── Unit/                       # Unit tests
├── Integration/                # Integration tests
└── Performance/                # Performance tests
```

## Configuration

### Application Settings
Configure the API behavior through `appsettings.json`:

```json
{
  "GameOfLife": {
    "DefaultMaxDimension": 1000,
    "DefaultMaxIterations": 1000,
    "DefaultStableStateThreshold": 20,
    "ProgressLoggingInterval": 100,
    "MaxCycleDetectionLength": 10,
    "CycleStabilityRequirement": 3
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gameoflife.db"
  }
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Sets the environment (Development, Production, etc.)
- `ConnectionStrings__DefaultConnection`: Database connection string

## Testing

The project includes comprehensive test coverage:

### Test Categories
- **Unit Tests**: Domain logic, services, and validators
- **Integration Tests**: Full API workflow testing
- **Functional Requirements Tests**: Verification of all requirements
- **Performance Tests**: Load and stress testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

## Performance

### Optimizations
- **Database Indexing**: Optimized indexes for query patterns
- **Entity Framework**: No-tracking queries, bulk operations
- **Memory Management**: Efficient cell coordinate storage
- **Algorithms**: Optimized Conway's rule implementation

### Performance Characteristics
- **Board Creation**: Sub-millisecond for typical boards
- **Generation Calculation**: Optimized for large boards (1000x1000)
- **Pattern Detection**: Efficient cycle detection algorithms
- **Database Operations**: Bulk insert/update for better throughput

## Monitoring & Logging

### Structured Logging
The application uses Serilog for comprehensive logging:
- **Request/Response Logging**: All API calls are logged
- **Performance Metrics**: Execution times and resource usage
- **Error Tracking**: Detailed error information with correlation IDs
- **Business Events**: Domain-specific events (pattern detection, etc.)

### Log Levels
- **Information**: Normal operations, API requests
- **Warning**: Validation failures, resource limits
- **Error**: Unhandled exceptions, system errors
- **Debug**: Detailed execution flow (development only)

## Error Handling

### Standardized Error Responses
All errors return consistent JSON responses:

```json
{
  "message": "Descriptive error message",
  "errorCode": "VALIDATION_ERROR",
  "validationErrors": ["Specific validation failures"],
  "timestamp": "2025-01-01T00:00:00Z"
}
```

### Error Categories
- **400 Bad Request**: Validation errors, invalid input
- **404 Not Found**: Non-existent board IDs
- **500 Internal Server Error**: System errors with correlation ID

## Security Considerations

### Input Validation
- **Board Size Limits**: Configurable maximum dimensions
- **Cell Count Limits**: Prevents resource exhaustion
- **Generation Limits**: Configurable iteration boundaries
- **Request Size Limits**: Built-in ASP.NET Core protections

### Data Protection
- **SQL Injection**: Protected by Entity Framework parameterization
- **No Sensitive Data**: No authentication or personal data stored
- **Error Information**: Sanitized error responses in production

## Extensibility

### Planned Enhancements
- **Pattern Library**: Pre-defined Conway patterns (gliders, guns, etc.)
- **Board Templates**: Common starting configurations
- **WebSocket Support**: Real-time board evolution streaming
- **Caching Layer**: Redis for frequently accessed patterns

### Customization Points
- **Rules Engine**: Configurable cellular automaton rules
- **Storage Providers**: Pluggable storage implementations
- **Pattern Detectors**: Custom pattern recognition algorithms
- **Export Formats**: Additional serialization formats

