using UnityEngine;

/// <summary>
/// Main UI manager - initializes top bar and result dialog.
/// </summary>
public class GameUI : MonoBehaviour
{
    private TopBarView _topBar;
    private ResultDialog _resultDialog;
    private GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;

        // Create top bar
        GameObject topBarGO = new GameObject("TopBarView");
        topBarGO.transform.SetParent(transform);
        _topBar = topBarGO.AddComponent<TopBarView>();
        _topBar.Init(gameManager);

        // Create result dialog (hidden initially)
        GameObject resultGO = new GameObject("ResultDialog");
        resultGO.transform.SetParent(transform);
        _resultDialog = resultGO.AddComponent<ResultDialog>();
        _resultDialog.Init(gameManager);
    }

    public void UpdateTopBar(int suitcases, int steps)
    {
        _topBar?.UpdateTopBar(suitcases, steps);
    }

    public void ShowResult(bool won)
    {
        _resultDialog?.Show(won);
    }

    /// <summary>
    /// Kick off the Phase D win sequence (board exit, "Great" text, big
    /// suitcase burst, retry/replay buttons). Called by GameManager when the
    /// victory barrier resolves. Lose still goes through ShowResult(false).
    /// </summary>
    public void PlayWinSequence(GameManager gm)
    {
        GameObject go = new GameObject("WinSequence");
        WinSequence ws = go.AddComponent<WinSequence>();
        ws.Init(gm);
        ws.Play();
    }

    /// <summary>World position of the target icon (flyer destination).</summary>
    public UnityEngine.Vector3 GetTargetWorldPosition()
    {
        return _topBar != null ? _topBar.GetTargetWorldPosition() : UnityEngine.Vector3.zero;
    }

    /// <summary>Pop the target icon + number when a flyer lands.</summary>
    public void BounceTarget()
    {
        _topBar?.Bounce();
    }
}
