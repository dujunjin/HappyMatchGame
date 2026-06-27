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
    /// Merge overlapping match groups so no cell appears in multiple groups.
    /// </summary>
    private List<List<(int, int)>> MergeMatches(List<List<(int, int)>> matches)
    {
        var merged = new List<List<(int, int)>>();
        var used = new HashSet<(int, int)>();

        foreach (var match in matches)
        {
            var filtered = match.Where(cell => !used.Contains(cell)).ToList();
            if (filtered.Count >= 3)
            {
                merged.Add(filtered);
                foreach (var cell in filtered)
                    used.Add(cell);
            }
        }

        return merged;
    }

    /// <summary>
    /// Check if a swap at (r1,c1) and (r2,c2) would produce any matches.
    /// </summary>
    public bool WouldSwapProduceMatch(int r1, int c1, int r2, int c2)
    {
        // Temporarily swap
        var tempType = _board.Cells[r1, c1].elementType;
        var tempSpecial = _board.Cells[r1, c1].specialType;
        _board.Cells[r1, c1].elementType = _board.Cells[r2, c2].elementType;
        _board.Cells[r1, c1].specialType = _board.Cells[r2, c2].specialType;
        _board.Cells[r2, c2].elementType = tempType;
        _board.Cells[r2, c2].specialType = tempSpecial;

        bool hasMatch = FindAllMatches().Count > 0;

        // Swap back
        _board.Cells[r2, c2].elementType = _board.Cells[r1, c1].elementType;
        _board.Cells[r2, c2].specialType = _board.Cells[r1, c1].specialType;
        _board.Cells[r1, c1].elementType = tempType;
        _board.Cells[r1, c1].specialType = tempSpecial;

        return hasMatch;
    }

    /// <summary>
    /// Detect special-item patterns in the matched cells.
    /// 4+ in a single line -> rocket (direction follows the line orientation).
    /// T/L junctions at match intersections -> bomb.
    /// Returns: (rocket cells with direction, bomb center cells)
    /// </summary>
    public (List<(int row, int col, GameConfig.RocketDir dir)> rockets, List<(int row, int col)> bombs) DetectSpecialPatterns(List<List<(int, int)>> matches)
    {
        var rockets = new List<(int, int, GameConfig.RocketDir)>();
        var bombs = new List<(int, int)>();

        // Count how many matches each cell belongs to (for intersection detection)
        var matchCount = new Dictionary<(int, int), int>();
        var matchedSet = new HashSet<(int, int)>();
        foreach (var match in matches)
        {
            foreach (var cell in match)
            {
                if (!matchCount.ContainsKey(cell))
                    matchCount[cell] = 0;
                matchCount[cell]++;
                matchedSet.Add(cell);
            }
        }

        // Bombs: cells at T/L junctions (belong to 2+ matches with crossing arms)
        var bombCells = new HashSet<(int, int)>();
        foreach (var kvp in matchCount)
        {
            if (kvp.Value >= 2)
            {
                var (r, c) = kvp.Key;
                if (IsTJunction(r, c, matchedSet) || IsLJunction(r, c, matchedSet))
                    bombCells.Add((r, c));
            }
        }
        bombs = new List<(int, int)>(bombCells);

        // Rockets: any match of length >= 4 -> one rocket at a non-bomb cell.
        // Direction follows the line: same row = horizontal, otherwise vertical.
        var rocketCells = new HashSet<(int, int)>();
        foreach (var match in matches)
        {
            if (match.Count < 4) continue;

            bool horizontal = true;
            for (int i = 1; i < match.Count; i++)
            {
                if (match[i].Item1 != match[0].Item1) { horizontal = false; break; }
            }
            var dir = horizontal ? GameConfig.RocketDir.Horizontal : GameConfig.RocketDir.Vertical;

            // Prefer a cell that isn't already claimed as a bomb or rocket.
            var chosen = match[0];
            foreach (var c in match)
            {
                if (!bombCells.Contains(c) && !rocketCells.Contains(c))
                {
                    chosen = c;
                    break;
                }
            }
            rocketCells.Add(chosen);
            rockets.Add((chosen.Item1, chosen.Item2, dir));
        }

        return (rockets, bombs);
    }

    private bool IsTJunction(int row, int col, HashSet<(int, int)> set)
    {
        bool left = Helper.IsInBounds(row, col - 1, _board.Rows, _board.Cols) && set.Contains((row, col - 1));
        bool right = Helper.IsInBounds(row, col + 1, _board.Rows, _board.Cols) && set.Contains((row, col + 1));
        bool up = Helper.IsInBounds(row - 1, col, _board.Rows, _board.Cols) && set.Contains((row - 1, col));
        bool down = Helper.IsInBounds(row + 1, col, _board.Rows, _board.Cols) && set.Contains((row + 1, col));

        int arms = (left ? 1 : 0) + (right ? 1 : 0) + (up ? 1 : 0) + (down ? 1 : 0);
        return arms >= 3;
    }

    private bool IsLJunction(int row, int col, HashSet<(int, int)> set)
    {
        bool inHorizontal = (Helper.IsInBounds(row, col - 1, _board.Rows, _board.Cols) && set.Contains((row, col - 1))) ||
                            (Helper.IsInBounds(row, col + 1, _board.Rows, _board.Cols) && set.Contains((row, col + 1)));
        bool inVertical = (Helper.IsInBounds(row - 1, col, _board.Rows, _board.Cols) && set.Contains((row - 1, col))) ||
                          (Helper.IsInBounds(row + 1, col, _board.Rows, _board.Cols) && set.Contains((row + 1, col)));

        return inHorizontal && inVertical;
    }
}
