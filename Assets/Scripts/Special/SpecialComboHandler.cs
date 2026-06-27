using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Resolves the three special×special combos defined by the spec:
///   - Rocket × Rocket : two full crosses (row+col through each rocket).
///   - Rocket × Bomb   : "fat line" — 3×3 around every cell on the rocket's
///                        line, plus the bomb's own 3×3.
///   - Rocket × Propeller : the rocket's line + the propeller's target
///                        (highest-priority suitcase, else random normal)
///                        and its 4-neighbors.
///
/// The two source specials are always consumed. The combined influence set is
/// deduplicated (same cell cleared once) and suitcases in it are routed
/// through TargetPresentation so they count toward the goal exactly once.
/// Other special pairs (bomb×bomb, bomb×propeller, propeller×propeller) are
/// out of spec scope and fall back to clearing just the two source cells.
/// </summary>
public class SpecialComboHandler
{
    private readonly BoardController _board;
    private readonly GameManager _gameManager;

    public SpecialComboHandler(BoardController board, GameManager gameManager)
    {
        _board = board;
        _gameManager = gameManager;
    }

    /// <summary>
    /// True if swapping (r1,c1) and (r2,c2) would trigger one of the three
    /// supported combos — i.e. both cells are special and at least one is a
    /// rocket (all three supported combos involve a rocket).
    /// </summary>
    public bool IsCombo(int r1, int c1, int r2, int c2)
    {
        if (!Helper.IsInBounds(r1, c1, _board.Rows, _board.Cols)) return false;
        if (!Helper.IsInBounds(r2, c2, _board.Rows, _board.Cols)) return false;
        var s1 = _board.Cells[r1, c1].specialType;
        var s2 = _board.Cells[r2, c2].specialType;
        if (s1 == GameConfig.SpecialType.None || s2 == GameConfig.SpecialType.None) return false;

        bool hasRocket = s1 == GameConfig.SpecialType.Rocket || s2 == GameConfig.SpecialType.Rocket;
        if (!hasRocket) return false;

        // Supported pairs (order-independent): {Rocket,Rocket}, {Rocket,Bomb},
        // {Rocket,Propeller}. Any rocket-involving pair qualifies.
        return true;
    }

    public IEnumerator ActivateCombo(int r1, int c1, int r2, int c2)
    {
        _gameManager.SetState(GameState.Clearing);

        var s1 = _board.Cells[r1, c1].specialType;
        var s2 = _board.Cells[r2, c2].specialType;

        var clear = new HashSet<(int, int)>();
        // Sources are always consumed.
        AddCell(clear, r1, c1);
        AddCell(clear, r2, c2);

        if (s1 == GameConfig.SpecialType.Rocket && s2 == GameConfig.SpecialType.Rocket)
        {
            // Two full crosses.
            AddCross(clear, r1, c1);
            AddCross(clear, r2, c2);
        }
        else
        {
            // Find the rocket source and the other special.
            int rr, rc, or_, oc;
            GameConfig.RocketDir rdir;
            if (s1 == GameConfig.SpecialType.Rocket)
            {
                (rr, rc) = (r1, c1); (or_, oc) = (r2, c2);
                rdir = _board.Cells[r1, c1].rocketDir;
            }
            else
            {
                (rr, rc) = (r2, c2); (or_, oc) = (r1, c1);
                rdir = _board.Cells[r2, c2].rocketDir;
            }

            var otherType = s1 == GameConfig.SpecialType.Rocket ? s2 : s1;

            if (otherType == GameConfig.SpecialType.Bomb)
            {
                // Fat line: 3x3 around every cell on the rocket's line.
                AddFatLine(clear, rr, rc, rdir);
                // Plus the bomb's own 3x3 (in case the bomb sits off-line).
                Add3x3(clear, or_, oc);
            }
            else if (otherType == GameConfig.SpecialType.Propeller)
            {
                // Rocket line + propeller target.
                AddLine(clear, rr, rc, rdir);
                AddPropellerTarget(clear);
            }
        }

        // Collect suitcases for counting (before destruction).
        var hitSuitcases = new List<(int, int)>();
        foreach (var (r, c) in clear)
            if (_board.Cells[r, c].elementType == ElementType.Suitcase)
                hitSuitcases.Add((r, c));

        // Flash + clear.
        yield return Flash(clear);

        foreach (var (r, c) in clear)
            _board.DestroyCell(r, c);

        if (hitSuitcases.Count > 0 && _gameManager.targetPresentation != null)
            _gameManager.targetPresentation.OnSuitcaseHit(hitSuitcases.Count, hitSuitcases);

        // A combo is a swap-move: costs one step.
        _gameManager.DecreaseStep();

        // Cascade + dead-board check via the presenter.
        if (_gameManager.boardPresenter != null)
            yield return _gameManager.boardPresenter.PresentCascade();
        else
            yield return _gameManager.cascadeManager.RunCascade();
    }

    // ---------------------------------------------------------------------
    //  Influence-set helpers
    // ---------------------------------------------------------------------

    private void AddCell(HashSet<(int, int)> set, int r, int c)
    {
        if (Helper.IsInBounds(r, c, _board.Rows, _board.Cols))
            set.Add((r, c));
    }

    private void AddCross(HashSet<(int, int)> set, int r, int c)
    {
        for (int i = 0; i < _board.Cols; i++) set.Add((r, i));
        for (int i = 0; i < _board.Rows; i++) set.Add((i, c));
    }

    private void AddLine(HashSet<(int, int)> set, int r, int c, GameConfig.RocketDir dir)
    {
        if (dir == GameConfig.RocketDir.Horizontal)
            for (int i = 0; i < _board.Cols; i++) set.Add((r, i));
        else
            for (int i = 0; i < _board.Rows; i++) set.Add((i, c));
    }

    private void Add3x3(HashSet<(int, int)> set, int r, int c)
    {
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
                AddCell(set, r + dr, c + dc);
    }

    private void AddFatLine(HashSet<(int, int)> set, int r, int c, GameConfig.RocketDir dir)
    {
        if (dir == GameConfig.RocketDir.Horizontal)
        {
            for (int i = 0; i < _board.Cols; i++)
                Add3x3(set, r, i);
        }
        else
        {
            for (int i = 0; i < _board.Rows; i++)
                Add3x3(set, i, c);
        }
    }

    /// <summary>
    /// Add the propeller's target (topmost-leftmost suitcase, else a random
    /// normal cell) and its 4-neighbors to the clear set.
    /// </summary>
    private void AddPropellerTarget(HashSet<(int, int)> set)
    {
        var suitcases = _board.GetSuitcaseCells();
        int tr, tc;
        if (suitcases.Count > 0)
        {
            (tr, tc) = suitcases[0];
        }
        else
        {
            var normals = new List<(int, int)>();
            for (int r = 0; r < _board.Rows; r++)
                for (int c = 0; c < _board.Cols; c++)
                {
                    var cell = _board.Cells[r, c];
                    if (!cell.IsEmpty && !cell.HasSpecial && cell.elementType != ElementType.Suitcase)
                        normals.Add((r, c));
                }
            if (normals.Count == 0) return;
            (tr, tc) = normals[Random.Range(0, normals.Count)];
        }

        AddCell(set, tr, tc);
        AddCell(set, tr - 1, tc);
        AddCell(set, tr + 1, tc);
        AddCell(set, tr, tc - 1);
        AddCell(set, tr, tc + 1);
    }

    private IEnumerator Flash(HashSet<(int, int)> cells)
    {
        float duration = 0.16f;
        var items = new List<SpriteRenderer>();
        foreach (var (r, c) in cells)
        {
            if (_board.Cells[r, c].gameObject == null) continue;
            var sr = _board.Cells[r, c].gameObject.GetComponent<SpriteRenderer>();
            if (sr != null) items.Add(sr);
        }
        if (items.Count == 0) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].transform.localScale = Vector3.one * s;
                    items[i].color = new Color(1f, 1f, 1f, 1f);
                }
            }
            yield return null;
        }
    }
}
