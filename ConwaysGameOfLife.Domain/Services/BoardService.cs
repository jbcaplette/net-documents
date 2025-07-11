using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ConwaysGameOfLife.Domain.Services;

public interface IBoardService
{
    Task<Board> CreateBoardAsync(IEnumerable<CellCoordinate> aliveCells, int maxDimension = 1000);
    Task<Board> GetNextStateAsync(BoardId boardId);
    Task<Board> GetStateAfterGenerationsAsync(BoardId boardId, int generations);
    Task<Board> GetFinalStateAsync(BoardId boardId, int maxIterations = 1000, int stableStateThreshold = 20);
}

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardHistoryRepository _boardHistoryRepository;
    private readonly ILogger<BoardService> _logger;

    public BoardService(
        IBoardRepository boardRepository, 
        IBoardHistoryRepository boardHistoryRepository,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardHistoryRepository = boardHistoryRepository;
        _logger = logger;
    }

    public async Task<Board> CreateBoardAsync(IEnumerable<CellCoordinate> aliveCells, int maxDimension = 1000)
    {
        var boardId = BoardId.NewId();
        var aliveCellsList = aliveCells.ToList();
        
        _logger.LogInformation("Creating new board {BoardId} with {AliveCellCount} alive cells and max dimension {MaxDimension}", 
            boardId.Value, aliveCellsList.Count, maxDimension);
        
        var board = new Board(boardId, aliveCellsList, maxDimension);
        
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
                if (i > 0 && (i + 1) % 100 == 0)
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

    public async Task<Board> GetFinalStateAsync(BoardId boardId, int maxIterations = 1000, int stableStateThreshold = 20)
    {
        _logger.LogInformation("Computing final state for board {BoardId} with max iterations {MaxIterations} and stable threshold {StableThreshold}", 
            boardId.Value, maxIterations, stableStateThreshold);

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
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                var stateHash = currentBoard.GetStateHash();
                stateHistory.Add(stateHash);
                
                // Log progress for long-running operations
                if (iteration > 0 && (iteration + 1) % 100 == 0)
                {
                    _logger.LogDebug("Final state progress: iteration {Iteration}/{MaxIterations} for board {BoardId}, generation {Generation}", 
                        iteration + 1, maxIterations, boardId.Value, currentBoard.Generation);
                }
                
                // Check for stability (same state)
                if (stateHistory.Count >= stableStateThreshold)
                {
                    var recentStates = stateHistory.TakeLast(stableStateThreshold);
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
                    if (cycleLength > 0 && HasStableCycle(stateHistory, cycleLength, stableStateThreshold))
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
                boardId.Value, maxIterations, currentBoard.Generation);
            
            throw new InvalidOperationException($"Board did not reach a stable state within {maxIterations} iterations");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Failed to compute final state for board {BoardId}", boardId.Value);
            throw;
        }
    }

    private static int DetectCycle(List<string> stateHistory)
    {
        var currentState = stateHistory.Last();
        
        for (int cycleLength = 1; cycleLength <= Math.Min(10, stateHistory.Count / 2); cycleLength++)
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

    private static bool HasStableCycle(List<string> stateHistory, int cycleLength, int stableThreshold)
    {
        if (stateHistory.Count < cycleLength * stableThreshold) return false;
        
        var cyclesToCheck = Math.Min(stableThreshold, stateHistory.Count / cycleLength);
        
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