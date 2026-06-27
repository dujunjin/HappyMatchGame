using UnityEngine;

/// <summary>
/// Authority for the match-3 state machine, step/suitcase counters, input
/// gating, and win/lose detection. Extracted from GameManager so the central
/// MonoBehaviour stays a thin orchestrator. GameManager delegates its
/// State/RemainingSteps/RemainingSuitcase APIs here.
///
/// Input gating:
///   - CanAcceptInput: normal swaps/clicks (Idle or Selecting only).
///   - CanActivateSpecial: rocket/bomb taps (Idle, Selecting, or Clearing).
///   During Swapping/Falling/Refilling/GameOver all ordinary input is
///   rejected so animations cannot be interrupted.
/// </summary>
public class GameFlowController
{
    public GameState State { get; private set; } = GameState.Idle;
    public int RemainingSteps { get; private set; }
    public int RemainingSuitcases { get; private set; }

    private GameUI _ui;
    private int _maxSteps;
    private int _targetSuitcases;

    public GameFlowController() { }

    /// <summary>
    /// Late-bind config + UI. Called from GameManager.Start once GameUI is
    /// available. Resets counters to the level's initial values.
    /// </summary>
    public void Init(LevelConfig config, GameUI ui)
    {
        if (config == null) config = LevelConfig.Default;
        _maxSteps = config.maxSteps;
        _targetSuitcases = config.targetSuitcaseCount;
        _ui = ui;
        RemainingSteps = _maxSteps;
        RemainingSuitcases = _targetSuitcases;
        State = GameState.Idle;
        _ui?.UpdateTopBar(RemainingSuitcases, RemainingSteps);
    }

    public bool CanAcceptInput => State == GameState.Idle || State == GameState.Selecting;
    public bool CanActivateSpecial => State == GameState.Idle || State == GameState.Selecting || State == GameState.Clearing;

    public void SetState(GameState newState)
    {
        State = newState;
    }

    public void DecreaseStep()
    {
        if (State == GameState.GameOver) return;
        int next = RemainingSteps - 1;
        RemainingSteps = next < 0 ? 0 : next;
        _ui?.UpdateTopBar(RemainingSuitcases, RemainingSteps);

        if (RemainingSteps <= 0 && RemainingSuitcases > 0)
        {
            SetState(GameState.GameOver);
            _ui?.ShowResult(false);
        }
    }

    public void DecreaseSuitcase(int count = 1)
    {
        if (State == GameState.GameOver) return;
        int next = RemainingSuitcases - count;
        RemainingSuitcases = next < 0 ? 0 : next;
        _ui?.UpdateTopBar(RemainingSuitcases, RemainingSteps);

        if (RemainingSuitcases <= 0)
        {
            SetState(GameState.GameOver);
            _ui?.ShowResult(true);
        }
    }
}
