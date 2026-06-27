using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages suitcase (target) items on the board.
/// Suitcases adjacent to matched cells get cleared and decrease the target count.
/// </summary>
public class SuitcaseManager
{
    private BoardController _board;

    public void Init(BoardController board)
    {
        _board = board;
    }

    /// <summary>
    /// Place initial suitcases randomly on the board.
    /// </summary>
    public void PlaceInitialSuitcases()
    {
        int count = GameConfig.InitialSuitcaseCount;
        int rows = _board.Rows;
        int cols = _board.Cols;

        // Collect all valid positions (cells with normal elements)
        var validPositions = new List<(int, int)>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (!_board.Cells[row, col].IsEmpty &&
                    _board.Cells[row, col].elementType != ElementType.Suitcase)
                {
                    validPositions.Add((row, col));
                }
            }
        }

        // Shuffle and pick positions
        Shuffle(validPositions);
        int toPlace = System.Math.Min(count, validPositions.Count);

        for (int i = 0; i < toPlace; i++)
        {
            var (row, col) = validPositions[i];

            // Store the element type underneath
            ElementType underlyingType = _board.Cells[row, col].elementType;

            // Update cell to suitcase
            _board.Cells[row, col].elementType = ElementType.Suitcase;
            _board.Cells[row, col].specialType = GameConfig.SpecialType.None;

            if (_board.Cells[row, col].gameObject != null)
            {
                var sr = _board.Cells[row, col].gameObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = GameManager.Instance.GetSpriteForType(ElementType.Suitcase);
                    // Tint slightly to show it's different
                    sr.color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }
    }

    /// <summary>
    /// Check suitcases adjacent to the given matched cells and clear them.
    /// </summary>
    public void CheckAdjacentSuitcases(List<(int row, int col)> matchedCells)
    {
        var toClear = new HashSet<(int, int)>();

        foreach (var (row, col) in matchedCells)
        {
            // Check 8 neighbors
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    int r = row + dr;
                    int c = col + dc;
                    if (Helper.IsInBounds(r, c, _board.Rows, _board.Cols))
                    {
                        if (_board.Cells[r, c].elementType == ElementType.Suitcase)
                        {
                            toClear.Add((r, c));
                        }
                    }
                }
            }
        }

        // Clear suitcases
        foreach (var (row, col) in toClear)
        {
            _board.DestroyCell(row, col);
        }

        // Update count
        if (toClear.Count > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.DecreaseSuitcase(toClear.Count);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
