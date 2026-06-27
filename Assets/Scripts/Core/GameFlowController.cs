using UnityEngine;

/// <summary>
/// Authority for the match-3 state machine, step/suitcase counters, input
/// gating, and win/lose detection. Extracted from GameManager.
///
/// Phase C: the suitcase counter is split into a LOGICAL count
/// (RemainingSuitcases, decremented immediately on hit — used for win
/// detection and to prevent double-hits) and a DISPLAY count (owned by
/// TargetPresentation, decremented when a flyer reaches the target bar).
/// Win and lose are NOT triggered immediately: DecreaseStep/DecreaseSuitcase
/// only set LosePending/WinPending. The actual result is triggered by
/// CheckEndGame() once the cascade has settled AND all in-flight flyers have
/// arrived (the "victory barrier" — spec §5.2.5). Win takes priority over
/// lose so a final-step cascade that clears the last suitcase still wins.
/// </summary>
public class GameFlowController
{
    public GameState State { get; private set; } = GameState.Idle;
    public int RemainingSteps { get; private set; }
    public int RemainingSuitcases { get; private set; }
    public bool WinPending { get; private set; }
    public bool LosePending { get; private set; }

    private GameManager _gm;
    private int _maxSteps;
    private int _targetSuitcases;

    public GameFlowController() { }

    public void Init(LevelConfig config, GameManager gm)
    {
        if (config == null) config = LevelConfig.Default;
        _gm = gm;
        _maxSteps = config.maxSteps;
        _targetSuitcases = config.targetSuitcaseCount;
        RemainingSteps = _maxSteps;
        RemainingSuitcases = _targetSuitcases;
        WinPending = false;
        LosePending = false;
        State = GameState.Idle;
        // RefreshTopBar is called by TargetPresentation.Init after the display
        // count is initialized; Flow.Init just sets the logical values.
    }

    /// <summary>
    /// Normal swaps/clicks: only when Idle/Selecting and no end-game is
    /// pending. WinPending/LosePending block input so the player can't act
    /// between the logical clear and the flyer arrivals / end-game trigger.
    /// </summary>
    public bool CanAcceptInput =>
        (State == GameState.Idle || State == GameState.Selecting) &&
        !WinPending && !LosePending;

    /// <summary>
    /// Specials (rocket/bomb/propeller) may still chain during a cascade
    /// (Clearing). Blocked during Swapping/Falling/Refilling/Settling/GameOver.
    /// </summary>
    public bool CanActivateSpecial =>
        State == GameState.Idle || State == GameState.Selecting || State == GameState.Clearing;

    public void SetState(GameState newState)
    {
        State = newState;
    }

    public void DecreaseStep()
    {
        if (State == GameState.GameOver) return;
        int next = RemainingSteps - 1;
        RemainingSteps = next < 0 ? 0 : next;
        _gm?.RefreshTopBar();

        // Defer the lose trigger: the cascade/flyers from this same swap might
        // still clear the last suitcase and win.
        if (RemainingSteps <= 0) LosePending = true;
    }

    public void DecreaseSuitcase(int count = 1)
    {
        if (State == GameState.GameOver) return;
        int next = RemainingSuitcases - count;
        RemainingSuitcases = next < 0 ? 0 : next;

        // Defer the win trigger until all flyers arrive (victory barrier).
        if (RemainingSuitcases <= 0) WinPending = true;
    }

    /// <summary>
    /// Called after the cascade settles AND all flyers have landed. Resolves
    /// the pending end state (win beats lose) or returns to Idle.
    /// </summary>
    public void CheckEndGame()
    {
        if (State == GameState.GameOver) return;
        if (WinPending) { TriggerWin(); return; }
        if (LosePending) { TriggerLose(); return; }
        SetState(GameState.Idle);
    }

    public void TriggerWin()
    {
        SetState(GameState.GameOver);
        // Phase D win sequence (board exit / Great / big suitcase / buttons).
        // Lose still uses the simple ResultDialog.
        _gm?.PlayWinSequence();
    }

    public void TriggerLose()
    {
        SetState(GameState.GameOver);
        _gm?.Audio?.Play(AudioCatalog.Event.Lose);
        _gm?.ShowResult(false);
    }
}
