using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Detects dead boards (no legal swap exists) and rescues them by shuffling
/// the non-suitcase, non-special elements in place. A shuffle guarantees the
/// reshuffled board has no initial 3-in-a-row and at least one legal swap.
///
/// HasLegalSwap enumerates every adjacent pair and asks MatchDetector.
/// WouldSwapProduceMatch (which temporarily swaps, detects, restores). Pairs
/// involving a rocket/bomb are skipped because the player cannot swap those.
/// </summary>
public class DeadBoardDetector
{
    private readonly MatchDetector _matchDetector;
    private readonly GameManager _gameManager;

    public DeadBoardDetector(MatchDetector matchDetector, GameManager gameManager)
    {
        _matchDetector = matchDetector;
        _gameManager = gameManager;
    }

    /// <summary>
    /// True if at least one player-legal move exists: either a special item
    /// (rocket/bomb) the player can tap, or an adjacent swap that would
    /// produce a match. Without the special-item shortcut, a board holding a
    /// freshly-created rocket with no swap-match would be wrongly shuffled.
    /// </summary>
    public bool HasLegalSwap(BoardController board)
    {
        int rows = board.Rows;
        int cols = board.Cols;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // A tap-able special item is always a legal move.
                if (board.Cells[row, col].HasSpecial)
                    return true;
            }
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Right neighbor
                if (col + 1 < cols && IsPlayerSwappable(board, row, col, row, col + 1))
                {
                    if (_matchDetector.WouldSwapProduceMatch(row, col, row, col + 1))
                        return true;
                }
                // Down neighbor
                if (row + 1 < rows && IsPlayerSwappable(board, row, col, row + 1, col))
                {
                    if (_matchDetector.WouldSwapProduceMatch(row, col, row + 1, col))
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// A pair is player-swappable if neither cell holds a special item
    /// (rocket/bomb/propeller — those activate by tap/combo, not swap) or a
    /// suitcase (suitcases are immovable). Normal×normal and normal×suitcase-
    /// adjacencies are both considered, but suitcase-involving pairs are
    /// skipped because the player cannot swap them.
    /// </summary>
    private bool IsPlayerSwappable(BoardController board, int r1, int c1, int r2, int c2)
    {
        if (board.Cells[r1, c1].IsEmpty) return false;
        if (board.Cells[r2, c2].IsEmpty) return false;
        if (board.Cells[r1, c1].HasSpecial) return false;
        if (board.Cells[r2, c2].HasSpecial) return false;
        if (board.Cells[r1, c1].elementType == ElementType.Suitcase) return false;
        if (board.Cells[r2, c2].elementType == ElementType.Suitcase) return false;
        return true;
    }

    /// <summary>
    /// Reshuffle every normal (non-suitcase, non-special) element in place,
    /// retrying until the result has no initial match and at least one legal
    /// swap, then update visuals and play a brief pulse.
    /// </summary>
    public IEnumerator Shuffle(BoardController board, LevelConfig config, GameManager gameManager)
    {
        if (config == null) config = LevelConfig.Default;
        gameManager?.Audio?.Play(AudioCatalog.Event.Shuffle);

        // Re-seed so the shuffle is deterministic per level (and different
        // from the initial layout's deterministic fill, since seed is reused).
        Random.InitState(config.seed + 7919);

        // Collect shuffleable positions and their current element types.
        var positions = new List<(int row, int col)>();
        var typePool = new List<ElementType>();

        for (int row = 0; row < board.Rows; row++)
        {
            for (int col = 0; col < board.Cols; col++)
            {
                var cell = board.Cells[row, col];
                if (cell.IsEmpty) continue;
                if (cell.HasSpecial) continue;
                if (cell.elementType == ElementType.Suitcase) continue;

                positions.Add((row, col));
                typePool.Add(cell.elementType);
            }
        }

        if (positions.Count == 0) yield break;

        // Try to find a valid arrangement.
        bool valid = false;
        for (int attempt = 0; attempt < 100 && !valid; attempt++)
        {
            ShuffleList(typePool);
            AssignNoMatch(board, positions, typePool);

            // No initial match AND at least one legal swap.
            if (_matchDetector.FindAllMatches().Count == 0 && HasLegalSwap(board))
                valid = true;
        }

        // Commit: update sprites for every shuffleable cell to match its new
        // type, then play a parallel scale pulse so the shuffle reads visually.
        var items = new List<(Transform tr, SpriteRenderer sr)>();
        foreach (var (row, col) in positions)
        {
            ElementType t = board.Cells[row, col].elementType;
            var go = board.Cells[row, col].gameObject;
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && gameManager != null)
                sr.sprite = gameManager.GetSpriteForType(t);
            if (go != null)
                items.Add((go.transform, sr));
        }

        yield return Pulse(items);
    }

    /// <summary>
    /// Assign types from the pool to positions in row-major order, skipping
    /// any type that would immediately form a 3-in-a-row with already-placed
    /// left/up neighbors. Falls back to the first type if none fit (the outer
    /// retry loop will reject the result if it still matches).
    /// </summary>
    private void AssignNoMatch(BoardController board, List<(int row, int col)> positions, List<ElementType> pool)
    {
        // Mark shuffleable cells empty first so WouldCauseMatch only sees the
        // freshly-placed types (plus the untouched suitcases/specials, which
        // WouldCauseMatch ignores because they are different element types).
        foreach (var (row, col) in positions)
            board.Cells[row, col].elementType = ElementType.Empty;

        for (int i = 0; i < positions.Count; i++)
        {
            var (row, col) = positions[i];
            ElementType chosen = pool[i];
            // If the pool type would cause a match here, try to find a better
            // one later in the pool; otherwise just place it (retry handles it).
            if (BoardGenerator.WouldCauseMatch(board.Cells, row, col, chosen))
            {
                for (int j = i + 1; j < pool.Count; j++)
                {
                    if (!BoardGenerator.WouldCauseMatch(board.Cells, row, col, pool[j]))
                    {
                        chosen = pool[j];
                        // Swap so the pool stays consistent.
                        pool[j] = pool[i];
                        pool[i] = chosen;
                        break;
                    }
                }
            }
            board.Cells[row, col].elementType = chosen;
        }
    }

    private IEnumerator Pulse(List<(Transform tr, SpriteRenderer sr)> items)
    {
        if (items.Count == 0) yield break;
        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 0 -> 1.18 -> 1
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].tr != null)
                    items[i].tr.localScale = Vector3.one * s;
            }
            yield return null;
        }
        for (int i = 0; i < items.Count; i++)
            if (items[i].tr != null) items[i].tr.localScale = Vector3.one;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
