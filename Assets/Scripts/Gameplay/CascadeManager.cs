using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Orchestrates the cascade loop: Clear → Gravity → Refill → Re-scan.
/// </summary>
public class CascadeManager
{
    private BoardController _board;
    private MatchDetector _matchDetector;
    private SwapHandler _swapHandler;
    private GravitySystem _gravitySystem;
    private SpecialFactory _specialFactory;
    private GameManager _gameManager;

    public void Init(BoardController board, MatchDetector matchDetector, SwapHandler swapHandler,
        GravitySystem gravitySystem, SpecialFactory specialFactory, GameManager gameManager)
    {
        _board = board;
        _matchDetector = matchDetector;
        _swapHandler = swapHandler;
        _gravitySystem = gravitySystem;
        _specialFactory = specialFactory;
        _gameManager = gameManager;
    }

    /// <summary>
    /// Run the full cascade loop after a swap.
    /// </summary>
    public IEnumerator RunCascade()
    {
        int iteration = 0;

        while (true)
        {
            // Step 1: Find all matches
            var matches = _matchDetector.FindAllMatches();

            if (matches.Count > 0)
            {
                _gameManager.SetState(GameState.Clearing);

                // Step 2: Detect special patterns (rockets carry their direction)
                var (rockets, bombs, propellers) = _matchDetector.DetectSpecialPatterns(matches);

                // Special cells are preserved and converted into click-able items,
                // so they must NOT be cleared with the rest of the match.
                var specialCells = new HashSet<(int, int)>();
                foreach (var (r, c, dir) in rockets) specialCells.Add((r, c));
                foreach (var (r, c) in bombs) specialCells.Add((r, c));
                foreach (var (r, c) in propellers) specialCells.Add((r, c));

                // Collect cells to clear (exclude special cells)
                var allClearCells = new HashSet<(int, int)>();
                foreach (var match in matches)
                {
                    foreach (var cell in match)
                    {
                        if (!specialCells.Contains(cell))
                            allClearCells.Add(cell);
                    }
                }

                // Check suitcases adjacent to cleared cells
                _gameManager.suitcaseManager.CheckAdjacentSuitcases(allClearCells.ToList());

                // Step 3: Animate clearing
                yield return AnimateClear(allClearCells.ToList(), iteration);

                // Step 4: Remove cleared cells
                foreach (var cell in allClearCells)
                {
                    var (cr, cc) = cell;
                    _board.DestroyCell(cr, cc);
                }

                // Step 5: Create special items at the preserved cells. The element
                // color at each cell is retained so the item keeps its color.
                foreach (var (row, col, dir) in rockets)
                {
                    yield return _specialFactory.CreateRocketAt(row, col, dir);
                }
                foreach (var (row, col) in bombs)
                {
                    yield return _specialFactory.CreateBombAt(row, col);
                }
                foreach (var (row, col) in propellers)
                {
                    yield return _specialFactory.CreatePropellerAt(row, col);
                }
            }

            // Always run gravity + refill when there are gaps on the board. This
            // also covers rocket/bomb activations that cleared cells without
            // forming a new match (otherwise those gaps would never be refilled).
            bool hasEmpty = _board.GetEmptyCells().Count > 0;
            if (matches.Count == 0 && !hasEmpty)
                break;

            // Step 6: Apply gravity
            _gameManager.SetState(GameState.Falling);
            var moves = _gravitySystem.ApplyGravity();
            yield return AnimateFalling(moves);

            // Step 7: Refill
            _gameManager.SetState(GameState.Refilling);
            var refillNeeds = _gravitySystem.GetRefillNeeds();
            yield return AnimateRefill(refillNeeds);

            iteration++;

            // Small delay between cascades
            yield return new WaitForSeconds(GameConfig.RefillDelay);
        }

        // Done cascading
        _gameManager.SetState(GameState.Idle);

        // Check win/lose after cascade
        if (_gameManager.State == GameState.GameOver) yield break;
    }

    private IEnumerator AnimateClear(List<(int, int)> cells, int iteration)
    {
        float baseDuration = GameConfig.ClearDuration;

        // Collect renderers so all cells fade/shrink in parallel (not one
        // after another). GameObjects are destroyed later by DestroyCell.
        var items = new List<(SpriteRenderer sr, Transform tr)>();
        foreach (var (row, col) in cells)
        {
            var cell = _board.Cells[row, col];
            if (cell.gameObject == null) continue;
            var sr = cell.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
                items.Add((sr, cell.gameObject.transform));
        }

        if (items.Count == 0)
        {
            yield return new WaitForSeconds(iteration * 0.02f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < baseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / baseDuration);
            for (int i = 0; i < items.Count; i++)
            {
                var (sr, tr) = items[i];
                if (sr != null)
                {
                    tr.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f - t);
                }
            }
            yield return null;
        }
    }

    private IEnumerator AnimateFalling(List<(int fromRow, int fromCol, int toRow, int toCol)> moves)
    {
        float duration = GameConfig.FallDuration;

        // Collect every move first, then tween them all in parallel over a
        // single duration (previously each fell one-by-one, which felt slow).
        var movers = new List<(Transform tr, Vector3 from, Vector3 to)>();
        foreach (var (fromRow, fromCol, toRow, toCol) in moves)
        {
            var cell = _board.Cells[toRow, toCol];
            if (cell.gameObject == null) continue;
            Vector3 from = _board.GetWorldPosition(fromRow, fromCol);
            Vector3 to = _board.GetWorldPosition(toRow, toCol);
            movers.Add((cell.gameObject.transform, from, to));
        }

        yield return AnimationHelper.TweenPositions(movers, duration);
    }

    private IEnumerator AnimateRefill(List<(int targetRow, int targetCol, ElementType type, GameConfig.SpecialType special, GameConfig.RocketDir rocketDir)> needs)
    {
        var movers = new List<(Transform tr, Vector3 from, Vector3 to)>();
        // Track the next spawn row per column so new elements stack above the
        // board (rows -1, -2, -3, ...) and fall in together without overlapping.
        var colStartRow = new Dictionary<int, int>();

        foreach (var (targetRow, targetCol, type, special, rocketDir) in needs)
        {
            if (!colStartRow.ContainsKey(targetCol))
                colStartRow[targetCol] = -1;
            int startRow = colStartRow[targetCol];
            colStartRow[targetCol] = startRow - 1;

            Vector3 startPos = _board.GetWorldPosition(startRow, targetCol);
            var go = _board.CreateFallingElement(type, startPos, special, rocketDir);

            // Update cell data
            _board.Cells[targetRow, targetCol].gameObject = go;
            _board.Cells[targetRow, targetCol].elementType = type;
            _board.Cells[targetRow, targetCol].specialType = special;
            _board.Cells[targetRow, targetCol].rocketDir = rocketDir;

            Vector3 targetPos = _board.GetWorldPosition(targetRow, targetCol);
            movers.Add((go.transform, startPos, targetPos));
        }

        // All new elements fall in together.
        yield return AnimationHelper.TweenPositions(movers, GameConfig.FallDuration);
    }
}
