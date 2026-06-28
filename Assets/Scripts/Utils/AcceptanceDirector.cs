using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Opt-in standalone acceptance choreography. It is inert in normal play and
/// runs only with -happyMatchAcceptance, producing deterministic visual proof.
/// </summary>
public class AcceptanceDirector : MonoBehaviour
{
    private GameManager _gameManager;
    private string _outputDirectory;

    public static bool ShouldRun(string[] args)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE"), "1", StringComparison.Ordinal))
            return true;
        if (args == null) return false;
        for (int i = 0; i < args.Length; i++)
            if (string.Equals(args[i], "-happyMatchAcceptance", StringComparison.Ordinal))
                return true;
        return false;
    }

    public static string OutputDirectory(string[] args)
    {
        string fromEnvironment = Environment.GetEnvironmentVariable("HAPPY_MATCH_ACCEPTANCE_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
        if (args != null)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], "-happyMatchAcceptanceDir", StringComparison.Ordinal))
                    return args[i + 1];
        }
        return Path.Combine(Application.persistentDataPath, "HappyMatchAcceptance");
    }

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
        _outputDirectory = OutputDirectory(Environment.GetCommandLineArgs());
        Directory.CreateDirectory(_outputDirectory);
        Application.targetFrameRate = 60;
        StartCoroutine(RunAcceptance());
    }

    private IEnumerator RunAcceptance()
    {
        Screen.SetResolution(820, 1022, false);
        yield return new WaitForSeconds(1.2f);
        yield return Capture("initial.png");

        List<(int row, int col)> cells = FindNormalCells(3);
        if (cells.Count == 3)
        {
            yield return _gameManager.specialFactory.CreateRocketAt(cells[0].row, cells[0].col, GameConfig.RocketDir.Horizontal);
            yield return _gameManager.specialFactory.CreateBombAt(cells[1].row, cells[1].col);
            yield return _gameManager.specialFactory.CreatePropellerAt(cells[2].row, cells[2].col);
            yield return new WaitForSeconds(0.25f);
            yield return Capture("specials.png");
        }

        RocketBehavior rocket = FindObjectOfType<RocketBehavior>();
        if (rocket != null)
        {
            rocket.SendMessage("OnMouseUpAsButton", SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(0.30f);
            yield return Capture("target-flight.png");
            yield return WaitForStable(4f);
        }

        BombBehavior bomb = FindObjectOfType<BombBehavior>();
        if (bomb != null)
        {
            bomb.SendMessage("OnMouseUpAsButton", SendMessageOptions.DontRequireReceiver);
            yield return WaitForStable(4f);
        }

        PropellerBehavior propeller = FindObjectOfType<PropellerBehavior>();
        if (propeller != null)
        {
            propeller.SendMessage("OnMouseUpAsButton", SendMessageOptions.DontRequireReceiver);
            yield return WaitForStable(4f);
        }

        yield return Capture("special-action.png");

        List<(int row, int col)> remaining = _gameManager.boardController.GetSuitcaseCells();
        if (remaining.Count > 0 && _gameManager.targetPresentation != null)
        {
            _gameManager.targetPresentation.OnSuitcaseHit(remaining.Count, remaining);
            yield return _gameManager.boardPresenter.PresentCascade();
        }

        yield return new WaitForSeconds(4.2f);
        yield return Capture("victory.png");
        File.WriteAllText(Path.Combine(_outputDirectory, "acceptance-complete.txt"),
            "Happy Match acceptance choreography completed at " + DateTime.UtcNow.ToString("O"));

        yield return new WaitForSeconds(0.5f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(0);
#endif
    }

    private List<(int row, int col)> FindNormalCells(int count)
    {
        var result = new List<(int, int)>();
        BoardController board = _gameManager.boardController;
        for (int row = 0; row < board.Rows && result.Count < count; row++)
        {
            for (int col = 0; col < board.Cols && result.Count < count; col++)
            {
                CellData cell = board.Cells[row, col];
                if (PolishMotion.IsColorMatchable(cell.elementType) && !cell.HasSpecial)
                    result.Add((row, col));
            }
        }
        return result;
    }

    private IEnumerator WaitForStable(float timeout)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            bool noFlyers = _gameManager.targetPresentation == null || !_gameManager.targetPresentation.HasInFlight;
            if ((_gameManager.State == GameState.Idle || _gameManager.State == GameState.GameOver) && noFlyers)
                yield break;
            yield return null;
        }
    }

    private IEnumerator Capture(string fileName)
    {
        string path = Path.Combine(_outputDirectory, fileName);
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.20f);
        Debug.Log("[AcceptanceDirector] captured: " + path);
    }
}
