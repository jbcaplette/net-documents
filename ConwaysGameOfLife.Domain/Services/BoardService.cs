using ConwaysGameOfLife.Domain.Entities;
using ConwaysGameOfLife.Domain.ValueObjects;

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

    public BoardService(IBoardRepository boardRepository, IBoardHistoryRepository boardHistoryRepository)
    {
        _boardRepository = boardRepository;
        _boardHistoryRepository = boardHistoryRepository;
    }

    public async Task<Board> CreateBoardAsync(IEnumerable<CellCoordinate> aliveCells, int maxDimension = 1000)
    {
        var boardId = BoardId.NewId();
        var board = new Board(boardId, aliveCells, maxDimension);
        
        await _boardRepository.SaveAsync(board);
        await _boardHistoryRepository.SaveAsync(new BoardHistory(board.Id, board.Generation, board.AliveCells));
        
        return board;
    }

    public async Task<Board> GetNextStateAsync(BoardId boardId)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");

        var nextBoard = board.NextGeneration();
        
        // Save the updated board state and history
        await _boardRepository.SaveAsync(nextBoard);
        await _boardHistoryRepository.SaveAsync(new BoardHistory(nextBoard.Id, nextBoard.Generation, nextBoard.AliveCells));
        
        return nextBoard;
    }

    public async Task<Board> GetStateAfterGenerationsAsync(BoardId boardId, int generations)
    {
        if (generations < 0)
            throw new ArgumentException("Generations must be non-negative", nameof(generations));

        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");

        var currentBoard = board;
        for (int i = 0; i < generations; i++)
        {
            currentBoard = currentBoard.NextGeneration();
            await _boardHistoryRepository.SaveAsync(new BoardHistory(currentBoard.Id, currentBoard.Generation, currentBoard.AliveCells));
        }

        // Save the final board state
        await _boardRepository.SaveAsync(currentBoard);
        return currentBoard;
    }

    public async Task<Board> GetFinalStateAsync(BoardId boardId, int maxIterations = 1000, int stableStateThreshold = 20)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
            throw new InvalidOperationException($"Board with ID {boardId.Value} not found");

        var stateHistory = new List<string>();
        var currentBoard = board;
        
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            var stateHash = currentBoard.GetStateHash();
            stateHistory.Add(stateHash);
            
            // Check for stability (same state)
            if (stateHistory.Count >= stableStateThreshold)
            {
                var recentStates = stateHistory.TakeLast(stableStateThreshold);
                if (recentStates.All(s => s == stateHash))
                {
                    // Save the final stable state
                    await _boardRepository.SaveAsync(currentBoard);
                    return currentBoard; // Stable state found
                }
            }
            
            // Check for cycles (oscillators)
            if (stateHistory.Count >= 4)
            {
                var cycleLength = DetectCycle(stateHistory);
                if (cycleLength > 0 && HasStableCycle(stateHistory, cycleLength, stableStateThreshold))
                {
                    // Save the final state in the cycle
                    await _boardRepository.SaveAsync(currentBoard);
                    return currentBoard; // Stable cycle found
                }
            }
            
            // Check if board is empty (all cells dead)
            if (currentBoard.IsEmpty)
            {
                // Save the empty board state
                await _boardRepository.SaveAsync(currentBoard);
                return currentBoard;
            }
            
            currentBoard = currentBoard.NextGeneration();
            await _boardHistoryRepository.SaveAsync(new BoardHistory(currentBoard.Id, currentBoard.Generation, currentBoard.AliveCells));
        }
        
        // Save the board state even if no stable state was found
        await _boardRepository.SaveAsync(currentBoard);
        throw new InvalidOperationException($"Board did not reach a stable state within {maxIterations} iterations");
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