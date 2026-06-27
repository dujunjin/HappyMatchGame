using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Detects matches on the board - horizontal, vertical, T/L patterns.
/// </summary>
public class MatchDetector
{
    private BoardController _board;

    public void Init(BoardController board)
    {
        _board = board;
    }

    /// <summary>
    /// Find all matches on the current board state.
    /// </summary>
    public List<List<(int row, int col)>> FindAllMatches()
    {
        var matches = new List<List<(int, int)>>();

        // Horizontal matches
        matches.AddRange(FindHorizontalMatches());

        // Vertical matches
        matches.AddRange(FindVerticalMatches());

        // Merge overlapping matches
        matches = MergeMatches(matches);

        return matches;
    }

    private List<List<(int, int)>> FindHorizontalMatches()
    {
        var matches = new List<List<(int, int)>>();
        int rows = _board.Rows;
        int cols = _board.Cols;

        for (int row = 0; row < rows; row++)
        {
            int runStart = -1;
            ElementType runType = ElementType.Empty;
            for (int col = 0; col <= cols; col++)
            {
                bool atEdge = col >= cols;
                ElementType current = !atEdge ? _board.Cells[row, col].elementType : ElementType.Empty;
                bool currentSpecial = !atEdge && _board.Cells[row, col].HasSpecial;

                // Only non-special, non-empty cells can be part of a color run.
                bool isRunCell = !atEdge && !currentSpecial && current != ElementType.Empty;

                if (!isRunCell || current != runType)
                {
                    if (runStart >= 0 && col - runStart >= 3)
                    {
                        var match = new List<(int, int)>();
                        for (int c = runStart; c < col; c++)
                            match.Add((row, c));
                        matches.Add(match);
                    }
                    runStart = isRunCell ? col : -1;
                    runType = isRunCell ? current : ElementType.Empty;
                }
            }
        }
        return matches;
    }

    private List<List<(int, int)>> FindVerticalMatches()
    {
        var matches = new List<List<(int, int)>>();
        int rows = _board.Rows;
        int cols = _board.Cols;

        for (int col = 0; col < cols; col++)
        {
            int runStart = -1;
            ElementType runType = ElementType.Empty;
            for (int row = 0; row <= rows; row++)
            {
                bool atEdge = row >= rows;
                ElementType current = !atEdge ? _board.Cells[row, col].elementType : ElementType.Empty;
                bool currentSpecial = !atEdge && _board.Cells[row, col].HasSpecial;

                bool isRunCell = !atEdge && !currentSpecial && current != ElementType.Empty;

                if (!isRunCell || current != runType)
                {
                    if (runStart >= 0 && row - runStart >= 3)
                    {
                        var match = new List<(int, int)>();
                        for (int r = runStart; r < row; r++)
                            match.Add((r, col));
                        matches.Add(match);
                    }
                    runStart = isRunCell ? row : -1;
                    runType = isRunCell ? current : ElementType.Empty;
                }
            }
        }
        return matches;
    }

    /// <summary>
    /// Merge overlapping match groups so a T/L shape stays in ONE group.
    /// Previously this filtered each match against already-used cells, which
    /// dropped the vertical arm of a T (its shared cell was already claimed
    /// by the horizontal run), leaving those cells uncleared and breaking
    /// bomb/junction detection. Now any matches sharing a cell are unioned.
    /// </summary>
    private List<List<(int, int)>> MergeMatches(List<List<(int, int)>> matches)
    {
        var groups = new List<HashSet<(int, int)>>();

        foreach (var match in matches)
        {
            if (match.Count == 0) continue;

            // Find existing groups that share any cell with this match.
            var overlapping = new List<HashSet<(int, int)>>();
            foreach (var g in groups)
            {
                bool overlaps = false;
                foreach (var cell in match)
                {
                    if (g.Contains(cell)) { overlaps = true; break; }
                }
                if (overlaps) overlapping.Add(g);
            }

            if (overlapping.Count == 0)
            {
                groups.Add(new HashSet<(int, int)>(match));
            }
            else
            {
                // Merge this match and all overlapping groups into the first.
                var target = overlapping[0];
                foreach (var cell in match) target.Add(cell);
                for (int i = 1; i < overlapping.Count; i++)
                {
                    foreach (var cell in overlapping[i]) target.Add(cell);
                    groups.Remove(overlapping[i]);
                }
            }
        }

        var merged = new List<List<(int, int)>>();
        foreach (var g in groups)
            if (g.Count >= 3)
                merged.Add(new List<(int, int)>(g));
        return merged;
    }

    /// <summary>
    /// Check if a swap at (r1,c1) and (r2,c2) would produce any matches.
    ///
    /// Semantics (used by DeadBoardDetector): temporarily swap the two cells'
    /// data, run FindAllMatches against that swapped state, then restore the
    /// original data so the board is left untouched. Detection is performed
    /// on the POST-swap state — which is exactly what a player-initiated swap
    /// would leave on the board.
    ///
    /// NOTE: callers that have already swapped the board's cell data (e.g.
    /// SwapHandler.SwapAndValidate) must NOT call this — it performs its own
    /// internal swap/restore and would otherwise detect against the wrong
    /// state. SwapHandler checks FindAllMatches directly for that reason.
    /// </summary>
    public bool WouldSwapProduceMatch(int r1, int c1, int r2, int c2)
    {
        // Snapshot originals (elementType + specialType + rocketDir so the
        // restore is a full round-trip even if future match logic consults
        // the rocket direction).
        ElementType typeA = _board.Cells[r1, c1].elementType;
        GameConfig.SpecialType specA = _board.Cells[r1, c1].specialType;
        GameConfig.RocketDir dirA = _board.Cells[r1, c1].rocketDir;

        ElementType typeB = _board.Cells[r2, c2].elementType;
        GameConfig.SpecialType specB = _board.Cells[r2, c2].specialType;
        GameConfig.RocketDir dirB = _board.Cells[r2, c2].rocketDir;

        // Apply the swap.
        _board.Cells[r1, c1].elementType = typeB;
        _board.Cells[r1, c1].specialType = specB;
        _board.Cells[r1, c1].rocketDir = dirB;
        _board.Cells[r2, c2].elementType = typeA;
        _board.Cells[r2, c2].specialType = specA;
        _board.Cells[r2, c2].rocketDir = dirA;

        bool hasMatch = FindAllMatches().Count > 0;

        // Restore originals (independent of detection result).
        _board.Cells[r1, c1].elementType = typeA;
        _board.Cells[r1, c1].specialType = specA;
        _board.Cells[r1, c1].rocketDir = dirA;
        _board.Cells[r2, c2].elementType = typeB;
        _board.Cells[r2, c2].specialType = specB;
        _board.Cells[r2, c2].rocketDir = dirB;

        return hasMatch;
    }

    /// <summary>
    /// Detect special-item patterns in the matched cells:
    ///   - Bomb  : a cell at the intersection of a 3+ horizontal run and a 3+
    ///             vertical run (a T or L of 5+ matched cells).
    ///   - Propeller : a 5+ straight line with no bomb cell.
    ///   - Rocket : a 4 straight line with no bomb cell.
    /// A merged group that contains a bomb cell yields a bomb (not a rocket/
    /// propeller) even if one of its arms is 4+/5+ long, which matches the
    /// standard match-3 rule (T/L -> bomb takes priority).
    /// Returns: (rocket cells with direction, bomb center cells, propeller cells).
    /// </summary>
    public (List<(int row, int col, GameConfig.RocketDir dir)> rockets,
           List<(int row, int col)> bombs,
           List<(int row, int col)> propellers) DetectSpecialPatterns(List<List<(int, int)>> matches)
    {
        var rockets = new List<(int, int, GameConfig.RocketDir)>();
        var bombs = new List<(int, int)>();
        var propellers = new List<(int, int)>();

        var matchedSet = new HashSet<(int, int)>();
        foreach (var match in matches)
            foreach (var cell in match)
                matchedSet.Add(cell);

        // Bombs: intersection of a 3+ horizontal run and a 3+ vertical run.
        var bombCells = new HashSet<(int, int)>();
        foreach (var (r, c) in matchedSet)
        {
            if (HorizontalRunLength(r, c, matchedSet) >= 3 &&
                VerticalRunLength(r, c, matchedSet) >= 3)
                bombCells.Add((r, c));
        }
        bombs = new List<(int, int)>(bombCells);

        // Rockets (4-line) and propellers (5+ line), but only for groups that
        // do NOT contain a bomb cell.
        var rocketCells = new HashSet<(int, int)>();
        var propellerCells = new HashSet<(int, int)>();

        foreach (var match in matches)
        {
            bool hasBomb = false;
            foreach (var cell in match)
                if (bombCells.Contains(cell)) { hasBomb = true; break; }
            if (hasBomb) continue;

            if (match.Count >= 5)
            {
                var chosen = ChooseSpecialCell(match, bombCells, propellerCells);
                propellerCells.Add(chosen);
                propellers.Add((chosen.Item1, chosen.Item2));
            }
            else if (match.Count >= 4)
            {
                bool horizontal = true;
                for (int i = 1; i < match.Count; i++)
                    if (match[i].Item1 != match[0].Item1) { horizontal = false; break; }
                var dir = horizontal ? GameConfig.RocketDir.Horizontal : GameConfig.RocketDir.Vertical;
                var chosen = ChooseSpecialCell(match, bombCells, rocketCells);
                rocketCells.Add(chosen);
                rockets.Add((chosen.Item1, chosen.Item2, dir));
            }
        }

        return (rockets, bombs, propellers);
    }

    /// <summary>Length of the consecutive matched run through (r,c) horizontally.</summary>
    private int HorizontalRunLength(int r, int c, HashSet<(int, int)> set)
    {
        int count = 1;
        for (int cc = c - 1; cc >= 0 && set.Contains((r, cc)); cc--) count++;
        for (int cc = c + 1; cc < _board.Cols && set.Contains((r, cc)); cc++) count++;
        return count;
    }

    /// <summary>Length of the consecutive matched run through (r,c) vertically.</summary>
    private int VerticalRunLength(int r, int c, HashSet<(int, int)> set)
    {
        int count = 1;
        for (int rr = r - 1; rr >= 0 && set.Contains((rr, c)); rr--) count++;
        for (int rr = r + 1; rr < _board.Rows && set.Contains((rr, c)); rr++) count++;
        return count;
    }

    /// <summary>
    /// Pick a cell in the match that is not already claimed as a bomb or same
    /// special type; falls back to match[0] (the cascade excludes special
    /// cells from clearing, so a duplicate claim is harmless but we avoid it
    /// for cleanliness).
    /// </summary>
    private (int, int) ChooseSpecialCell(List<(int, int)> match, HashSet<(int, int)> bombCells, HashSet<(int, int)> taken)
    {
        // Prefer the middle of the line for a centered placement.
        int mid = match.Count / 2;
        for (int off = 0; off < match.Count; off++)
        {
            int idx = (mid + off) % match.Count;
            var c = match[idx];
            if (!bombCells.Contains(c) && !taken.Contains(c))
                return c;
        }
        return match[0];
    }
}
