using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;
using ConwaysGameOfLife.Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConwaysGameOfLife.Domain.Services;

public interface IBoardService
{
    Task<Board> CreateBoardAsync(IEnumerable<CellCoordinate> aliveCells, int? maxDimension = null);
    Task<Board> GetNextStateAsync(BoardId boardId);
    Task<Board> GetStateAfterGenerationsAsync(BoardId boardId, int generations);
    Task<Board> GetFinalStateAsync(BoardId boardId, int? maxIterations = null, int? stableStateThreshold = null);
}

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardHistoryRepository _boardHistoryRepository;
    private readonly ILogger<BoardService> _logger;
    private readonly GameOfLifeSettings _settings;

    public BoardService(
        IBoardRepository boardRepository, 
        IBoardHistoryRepository boardHistoryRepository,
        ILogger<BoardService> logger,
        IOptions<GameOfLifeSettings> settings)
    {
        _boardRepository = boardRepository;
        _boardHistoryRepository = boardHistoryRepository;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Board> CreateBoardAsync(IEnumerable<CellCoordinate> aliveCells, int? maxDimension = null)
    {
        var boardId = BoardId.NewId();
        var aliveCellsList = aliveCells.ToList();
        var actualMaxDimension = maxDimension ?? _settings.DefaultMaxDimension;
        
        _logger.LogInformation("Creating new board {BoardId} with {AliveCellCount} alive cells and max dimension {MaxDimension}", 
            boardId.Value, aliveCellsList.Count, actualMaxDimension);
        
        var board = new Board(boardId, aliveCellsList, actualMaxDimension);
        
        try
        {
            await _boardRepository.SaveAsync(board);
            await _boardHistoryRepository.SaveAsync(new BoardHistory(board.Id, board.Generation, board.AliveCells));
            
            _logger.LogInformation("Successfully created and saved board {BoardId}", boardId.Value);
            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create board {BoardId}", boardId.Value);
            throw;
        }
    }

    public async Task<Board> GetNextStateAsync(BoardId boardId)
    {
        _logger.LogInformation("Computing next state for board {BoardId}", boardId.Value);
        
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            _logger.LogWarning("Board {BoardId} not found", boardId.Value);
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");
        }

        var currentGeneration = board.Generation;
        var nextBoard = board.NextGeneration();
        
        try
        {
            // Save the updated board state and history
            await _boardRepository.SaveAsync(nextBoard);
            await _boardHistoryRepository.SaveAsync(new BoardHistory(nextBoard.Id, nextBoard.Generation, nextBoard.AliveCells));
            
            _logger.LogInformation("Successfully computed next state for board {BoardId}: generation {OldGeneration} -> {NewGeneration}, alive cells: {AliveCellCount}", 
                boardId.Value, currentGeneration, nextBoard.Generation, nextBoard.AliveCells.Count);
            
            return nextBoard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute next state for board {BoardId}", boardId.Value);
            throw;
        }
    }

    public async Task<Board> GetStateAfterGenerationsAsync(BoardId boardId, int generations)
    {
        if (generations < 0)
        {
            _logger.LogWarning("Invalid generations parameter {Generations} for board {BoardId}", generations, boardId.Value);
            throw new ArgumentException("Generations must be non-negative", nameof(generations));
        }

        _logger.LogInformation("Computing {Generations} generations ahead for board {BoardId}", generations, boardId.Value);

        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            _logger.LogWarning("Board {BoardId} not found", boardId.Value);
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");
        }

        var currentBoard = board;
        var startGeneration = currentBoard.Generation;
        
        try
        {
            for (int i = 0; i < generations; i++)
            {
                currentBoard = currentBoard.NextGeneration();
                await _boardHistoryRepository.SaveAsync(new BoardHistory(currentBoard.Id, currentBoard.Generation, currentBoard.AliveCells));
                
                // Log progress for long-running operations
                if (i > 0 && (i + 1) % _settings.ProgressLoggingInterval == 0)
                {
                    _logger.LogInformation("Progress: computed {CompletedGenerations}/{TotalGenerations} generations for board {BoardId}", 
                        i + 1, generations, boardId.Value);
                }
            }

            // Save the final board state
            await _boardRepository.SaveAsync(currentBoard);
            
            _logger.LogInformation("Successfully computed {Generations} generations for board {BoardId}: generation {StartGeneration} -> {EndGeneration}, alive cells: {AliveCellCount}", 
                generations, boardId.Value, startGeneration, currentBoard.Generation, currentBoard.AliveCells.Count);
            
            return currentBoard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute {Generations} generations for board {BoardId}", generations, boardId.Value);
            throw;
        }
    }

    public async Task<Board> GetFinalStateAsync(BoardId boardId, int? maxIterations = null, int? stableStateThreshold = null)
    {
        var actualMaxIterations = maxIterations ?? _settings.DefaultMaxIterations;
        var actualStableThreshold = stableStateThreshold ?? _settings.DefaultStableStateThreshold;
        
        _logger.LogInformation("Computing final state for board {BoardId} with max iterations {MaxIterations} and stable threshold {StableThreshold}", 
            boardId.Value, actualMaxIterations, actualStableThreshold);

        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            _logger.LogWarning("Board {BoardId} not found", boardId.Value);
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");
        }

        var stateHistory = new List<string>();
        var currentBoard = board;
        var startGeneration = currentBoard.Generation;
        
        try
        {
            for (int iteration = 0; iteration < actualMaxIterations; iteration++)
            {
                var stateHash = currentBoard.GetStateHash();
                stateHistory.Add(stateHash);
                
                // Log progress for long-running operations
                if (iteration > 0 && (iteration + 1) % _settings.ProgressLoggingInterval == 0)
                {
                    _logger.LogDebug("Final state progress: iteration {Iteration}/{MaxIterations} for board {BoardId}, generation {Generation}", 
                        iteration + 1, actualMaxIterations, boardId.Value, currentBoard.Generation);
                }
                
                // Check for stability (same state)
                if (stateHistory.Count >= actualStableThreshold)
                {
                    var recentStates = stateHistory.TakeLast(actualStableThreshold);
                    if (recentStates.All(s => s == stateHash))
                    {
                        await _boardRepository.SaveAsync(currentBoard);
                        _logger.LogInformation("Stable state found for board {BoardId} at generation {Generation} after {Iterations} iterations", 
                            boardId.Value, currentBoard.Generation, iteration + 1);
                        return currentBoard; // Stable state found
                    }
                }
                
                // Check for cycles (oscillators)
                if (stateHistory.Count >= 4)
                {
                    var cycleLength = DetectCycle(stateHistory);
                    if (cycleLength > 0 && HasStableCycle(stateHistory, cycleLength, actualStableThreshold))
                    {
                        await _boardRepository.SaveAsync(currentBoard);
                        _logger.LogInformation("Stable cycle (length {CycleLength}) found for board {BoardId} at generation {Generation} after {Iterations} iterations", 
                            cycleLength, boardId.Value, currentBoard.Generation, iteration + 1);
                        return currentBoard; // Stable cycle found
                    }
                }
                
                // Check if board is empty (all cells dead)
                if (currentBoard.IsEmpty)
                {
                    await _boardRepository.SaveAsync(currentBoard);
                    _logger.LogInformation("Empty board state reached for board {BoardId} at generation {Generation} after {Iterations} iterations", 
                        boardId.Value, currentBoard.Generation, iteration + 1);
                    return currentBoard;
                }
                
                currentBoard = currentBoard.NextGeneration();
                await _boardHistoryRepository.SaveAsync(new BoardHistory(currentBoard.Id, currentBoard.Generation, currentBoard.AliveCells));
            }
            
            // Save the board state even if no stable state was found
            await _boardRepository.SaveAsync(currentBoard);
            
            _logger.LogWarning("Board {BoardId} did not reach a stable state within {MaxIterations} iterations. Final generation: {FinalGeneration}", 
                boardId.Value, actualMaxIterations, currentBoard.Generation);
            
            throw new InvalidOperationException($"Board did not reach a stable state within {actualMaxIterations} iterations");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Failed to compute final state for board {BoardId}", boardId.Value);
            throw;
        }
    }

    private int DetectCycle(List<string> stateHistory)
    {
        var currentState = stateHistory.Last();
        
        for (int cycleLength = 1; cycleLength <= Math.Min(_settings.MaxCycleDetectionLength, stateHistory.Count / 2); cycleLength++)
        {
            if (stateHistory.Count < cycleLength * 2) continue;
            
            var isCycle = true;
            for (int i = 1; i <= cycleLength; i++)
            {
                if (stateHistory[^i] != stateHistory[^(i + cycleLength)])
                {
                    isCycle = false;
                    break;
                }
            }
            
            if (isCycle) return cycleLength;
        }
        
        return 0;
    }

    private bool HasStableCycle(List<string> stateHistory, int cycleLength, int stableThreshold)
    {
        if (stateHistory.Count < cycleLength * _settings.CycleStabilityRequirement) return false;
        
        var cyclesToCheck = Math.Min(_settings.CycleStabilityRequirement, stateHistory.Count / cycleLength);
        
        for (int cycle = 0; cycle < cyclesToCheck; cycle++)
        {
            for (int i = 0; i < cycleLength; i++)
            {
                var index1 = stateHistory.Count - 1 - i - (cycle * cycleLength);
                var index2 = stateHistory.Count - 1 - i - ((cycle + 1) * cycleLength);
                
                if (index2 < 0 || stateHistory[index1] != stateHistory[index2])
                    return false;
            }
        }
        
        return true;
    }
}