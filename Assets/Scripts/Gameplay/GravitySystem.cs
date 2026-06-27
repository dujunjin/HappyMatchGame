using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles gravity: elements fall down after matches are cleared.
/// </summary>
public class GravitySystem
{
    private BoardController _board;

    public void Init(BoardController board)
    {
        _board = board;
    }

    /// <summary>
    /// Apply gravity - shift all elements down to fill gaps.
    /// Returns list of (row, col) for each element that moved, with their target positions.
    /// </summary>
    public List<(int fromRow, int fromCol, int toRow, int toCol)> ApplyGravity()
    {
        var moves = new List<(int, int, int, int)>();

        // Process each column
        for (int col = 0; col < _board.Cols; col++)
        {
            // Collect non-empty elements from bottom to top
            var elements = new List<(int row, CellData cell)>();
            for (int row = _board.Rows - 1; row >= 0; row--)
            {
                if (!_board.Cells[row, col].IsEmpty)
                {
                    elements.Add((row, _board.Cells[row, col]));
                }
            }

            // Place them at the bottom
            int targetRow = _board.Rows - 1;
            foreach (var (origRow, cell) in elements)
            {
                if (origRow != targetRow)
                {
                    moves.Add((origRow, col, targetRow, col));

                    // Move cell data
                    _board.Cells[targetRow, col] = cell;
                    _board.Cells[targetRow, col].row = targetRow;
                    _board.Cells[targetRow, col].col = col;

                    // Clear old position
                    _board.Cells[origRow, col] = new CellData
                    {
                        row = origRow,
                        col = col,
                        elementType = ElementType.Empty,
                        gameObject = null,
                        specialType = GameConfig.SpecialType.None
                    };
                }
                targetRow--;
            }
        }

        return moves;
    }

    /// <summary>
    /// Get elements that need new objects (above the board, to fall in).
    /// </summary>
    public List<(int targetRow, int targetCol, ElementType type, GameConfig.SpecialType special, GameConfig.RocketDir rocketDir)> GetRefillNeeds()
    {
        var needs = new List<(int, int, ElementType, GameConfig.SpecialType, GameConfig.RocketDir)>();
        int rows = _board.Rows;
        int cols = _board.Cols;

        for (int col = 0; col < cols; col++)
        {
            int emptyCount = 0;
            for (int row = 0; row < rows; row++)
            {
                if (_board.Cells[row, col].IsEmpty)
                    emptyCount++;
            }

            // Generate new elements for empty cells
            for (int i = 0; i < emptyCount; i++)
            {
                int targetRow = emptyCount - 1 - i;
                ElementType type = GameConfig.NormalElements[Random.Range(0, GameConfig.NormalElements.Length)];
                needs.Add((targetRow, col, type, GameConfig.SpecialType.None, GameConfig.RocketDir.Horizontal));
            }
        }

        return needs;
    }
}
