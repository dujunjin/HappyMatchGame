using UnityEngine;
using System.Collections;

/// <summary>
/// Translates logical results (swap, cascade) into animation sequences and
/// is the single place where future phases hook particles / sound. In Phase A
/// it is a thin wrapper over the existing SwapHandler.SwapAndValidate and
/// CascadeManager.RunCascade, plus a dead-board check after each cascade
/// settles: if no legal swap remains, it triggers a shuffle.
///
/// Callers (BoardInput, RocketBehavior, BombBehavior) should use
/// PresentCascade() instead of CascadeManager.RunCascade() directly so the
/// dead-board check and future presentation hooks always run.
/// </summary>
public class BoardPresenter
{
    private readonly GameManager _gameManager;
    private readonly SwapHandler _swapHandler;
    private readonly CascadeManager _cascadeManager;
    private readonly DeadBoardDetector _deadBoardDetector;
    private readonly BoardController _board;
    private readonly GameFlowController _flow;
    private readonly LevelConfig _levelConfig;

    public BoardPresenter(GameManager gameManager,
        SwapHandler swapHandler,
        CascadeManager cascadeManager,
        DeadBoardDetector deadBoardDetector,
        BoardController board,
        GameFlowController flow,
        LevelConfig levelConfig)
    {
        _gameManager = gameManager;
        _swapHandler = swapHandler;
        _cascadeManager = cascadeManager;
        _deadBoardDetector = deadBoardDetector;
        _board = board;
        _flow = flow;
        _levelConfig = levelConfig;
    }

    /// <summary>
    /// Present a swap and, if valid, run the cascade. Mirrors the
    /// BoardInput.ExecuteSwap sequence but routed through the presenter.
    /// </summary>
    public IEnumerator PresentSwap(int r1, int c1, int r2, int c2)
    {
        yield return _swapHandler.SwapAndValidate(r1, c1, r2, c2);

        if (_flow.State != GameState.Idle)
            _flow.SetState(GameState.Idle);

        yield return PresentCascade();
    }

    /// <summary>
    /// Run the cascade loop, then once the board has settled, check for a dead
    /// board and shuffle if no legal swap remains. Skipped when the game just
    /// ended (win/lose): RunCascade normalizes state to Idle, so the counters
    /// are the reliable end-of-game signal here.
    /// </summary>
    public IEnumerator PresentCascade()
    {
        yield return _cascadeManager.RunCascade();

        // Win/lose may have triggered during the cascade; don't shuffle then.
        if (_flow.RemainingSuitcases <= 0 || _flow.RemainingSteps <= 0) yield break;

        // Dead-board check + shuffle (animation + data reassign).
        if (_deadBoardDetector != null && !_deadBoardDetector.HasLegalSwap(_board))
        {
            yield return _deadBoardDetector.Shuffle(_board, _levelConfig, _gameManager);
        }
    }
}
