using UnityEngine;
using System.Collections;

/// <summary>
/// Handles element selection and swapping logic.
/// </summary>
public class SwapHandler
{
    private BoardController _board;
    private GameManager _gameManager;

    private (int row, int col)? _selectedCell;

    public void Init(BoardController board, GameManager gameManager)
    {
        _board = board;
        _gameManager = gameManager;
    }

    /// <summary>
    /// Select or swap a cell. Returns true if a swap should proceed.
    /// </summary>
    public bool HandleCellClick(int row, int col)
    {
        if (_gameManager.State != GameState.Idle) return false;

        if (_selectedCell == null)
        {
            _selectedCell = (row, col);
            HighlightCell(row, col, true);
            return false;
        }

        var prev = _selectedCell.Value;

        if (prev.row == row && prev.col == col)
        {
            HighlightCell(row, col, false);
            _selectedCell = null;
            return false;
        }

        if (IsAdjacent(prev.row, prev.col, row, col))
        {
            HighlightCell(prev.row, prev.col, false);
            _selectedCell = null;
            return true;
        }

        HighlightCell(prev.row, prev.col, false);
        _selectedCell = (row, col);
        HighlightCell(row, col, true);
        return false;
    }

    /// <summary>
    /// Handle drag: returns true if a drag-triggered swap should proceed.
    /// </summary>
    public bool HandleDrag(Vector3 worldStart, Vector3 worldEnd)
    {
        if (_gameManager.State != GameState.Idle) return false;

        var start = _board.WorldToGrid(worldStart);
        var end = _board.WorldToGrid(worldEnd);

        if (!Helper.IsInBounds(start.row, start.col, _board.Rows, _board.Cols)) return false;
        if (!Helper.IsInBounds(end.row, end.col, _board.Rows, _board.Cols)) return false;

        if (!IsAdjacent(start.row, start.col, end.row, end.col)) return false;
        if (start.row == end.row && start.col == end.col) return false;

        return true;
    }

    public (int row, int col)? GetDragSource(Vector3 worldPos)
    {
        var cell = _board.WorldToGrid(worldPos);
        if (Helper.IsInBounds(cell.row, cell.col, _board.Rows, _board.Cols))
            return cell;
        return null;
    }

    public (int row, int col)? GetDragTarget(Vector3 worldPos)
    {
        var cell = _board.WorldToGrid(worldPos);
        if (Helper.IsInBounds(cell.row, cell.col, _board.Rows, _board.Cols))
            return cell;
        return null;
    }

    private bool IsAdjacent(int r1, int c1, int r2, int c2)
    {
        return Mathf.Abs(r1 - r2) + Mathf.Abs(c1 - c2) == 1;
    }

    private void HighlightCell(int row, int col, bool on)
    {
        if (_board.Cells[row, col].gameObject == null) return;
        var sr = _board.Cells[row, col].gameObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = on ? new Color(1f, 1f, 1f, 0.7f) : Color.white;
        }
    }

    /// <summary>
    /// Execute swap animation and validation.
    /// Returns true if swap was valid (produced matches), false if swapped back.
    /// After returning, state is always Idle (valid) or Swapping→Idle (invalid).
    /// </summary>
    public IEnumerator SwapAndValidate(int r1, int c1, int r2, int c2)
    {
        _gameManager.SetState(GameState.Swapping);

        // Swap data
        SwapCellData(r1, c1, r2, c2);

        // Animate swap
        var go1 = _board.Cells[r2, c2].gameObject;
        var go2 = _board.Cells[r1, c1].gameObject;
        var pos1 = _board.GetWorldPosition(r2, c2);
        var pos2 = _board.GetWorldPosition(r1, c1);

        yield return AnimationHelper.TweenSwapPair(
            go1 != null ? go1.transform : null, go1 != null ? go1.transform.position : pos1, pos1,
            go2 != null ? go2.transform : null, go2 != null ? go2.transform.position : pos2, pos2,
            GameConfig.SwapDuration);

        // Validate: data is already swapped above, so check matches on the
        // current (post-swap) board state directly. Do NOT call
        // WouldSwapProduceMatch here — it performs its own internal swap,
        // which would revert to the pre-swap state and never find a match.
        bool valid = _gameManager.matchDetector.FindAllMatches().Count > 0;

        if (!valid)
        {
            // Swap back data
            SwapCellData(r1, c1, r2, c2);

            go1 = _board.Cells[r2, c2].gameObject;
            go2 = _board.Cells[r1, c1].gameObject;
            pos1 = _board.GetWorldPosition(r2, c2);
            pos2 = _board.GetWorldPosition(r1, c1);

            yield return AnimationHelper.PunchScales(
                go1 != null ? go1.transform : null,
                go2 != null ? go2.transform : null,
                0.08f, 0.07f);
            yield return new WaitForSeconds(0.04f);
            yield return AnimationHelper.TweenSwapPair(
                go1 != null ? go1.transform : null, go1 != null ? go1.transform.position : pos1, pos1,
                go2 != null ? go2.transform : null, go2 != null ? go2.transform.position : pos2, pos2,
                GameConfig.SwapDuration);

            _gameManager.Audio?.Play(AudioCatalog.Event.SwapInvalid);
            _gameManager.SetState(GameState.Idle);
            yield break;
        }

        // Valid swap
        _gameManager.Audio?.Play(AudioCatalog.Event.Swap);
        _gameManager.DecreaseStep();
        _gameManager.SetState(GameState.Idle);
    }

    private void SwapCellData(int r1, int c1, int r2, int c2)
    {
        var temp = _board.Cells[r1, c1];
        _board.Cells[r1, c1] = _board.Cells[r2, c2];
        _board.Cells[r2, c2] = temp;

        _board.Cells[r1, c1].row = r1;
        _board.Cells[r1, c1].col = c1;
        _board.Cells[r2, c2].row = r2;
        _board.Cells[r2, c2].col = c2;
    }
}
