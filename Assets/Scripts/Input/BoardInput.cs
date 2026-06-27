using UnityEngine;
using System.Collections;

/// <summary>
/// Handles board input: click-to-select (two taps) and drag-to-swap.
/// </summary>
public class BoardInput : MonoBehaviour
{
    private BoardController _board;
    private SwapHandler _swapHandler;
    private GameManager _gameManager;

    private (int row, int col)? _selectedCell;

    public void Init(BoardController board, SwapHandler swapHandler, GameManager gameManager)
    {
        _board = board;
        _swapHandler = swapHandler;
        _gameManager = gameManager;
    }

    private void Update()
    {
        if (_gameManager == null || _swapHandler == null) return;

        // Ignore input during animations
        GameState state = _gameManager.State;
        if (state != GameState.Idle && state != GameState.Selecting)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        if (Input.GetMouseButton(0))
        {
            HandleDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }
    }

    private bool _isDragging;
    private (int row, int col)? _dragSource;

    private void HandleClick()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        var cell = _board.WorldToGrid(worldPos);
        if (!Helper.IsInBounds(cell.row, cell.col, _board.Rows, _board.Cols))
        {
            Deselect();
            return;
        }

        if (_board.Cells[cell.row, cell.col].IsEmpty) return;
        if (_board.Cells[cell.row, cell.col].HasSpecial) return;

        if (_selectedCell == null)
        {
            _selectedCell = cell;
            _gameManager.SetState(GameState.Selecting);
            HighlightCell(cell.row, cell.col, true);
        }
        else
        {
            var prev = _selectedCell.Value;

            if (prev.row == cell.row && prev.col == cell.col)
            {
                Deselect();
                return;
            }

            if (IsAdjacent(prev.row, prev.col, cell.row, cell.col))
            {
                HighlightCell(prev.row, prev.col, false);
                _selectedCell = null;
                StartCoroutine(ExecuteSwap(prev.row, prev.col, cell.row, cell.col));
            }
            else
            {
                HighlightCell(prev.row, prev.col, false);
                _selectedCell = cell;
                HighlightCell(cell.row, cell.col, true);
            }
        }
    }

    private void HandleDrag()
    {
        if (_isDragging && _dragSource.HasValue)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;
            var target = _board.WorldToGrid(worldPos);

            if (Helper.IsInBounds(target.row, target.col, _board.Rows, _board.Cols) &&
                IsAdjacent(_dragSource.Value.row, _dragSource.Value.col, target.row, target.col))
            {
                _isDragging = false;
                StartCoroutine(ExecuteSwap(
                    _dragSource.Value.row, _dragSource.Value.col,
                    target.row, target.col));
                _dragSource = null;
            }
        }
        else if (!_isDragging && Input.GetMouseButton(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;
            var cell = _board.WorldToGrid(worldPos);

            if (Helper.IsInBounds(cell.row, cell.col, _board.Rows, _board.Cols) &&
                !_board.Cells[cell.row, cell.col].IsEmpty &&
                !_board.Cells[cell.row, cell.col].HasSpecial)
            {
                // If we have a selection, start drag from selected cell
                if (_selectedCell.HasValue)
                {
                    _dragSource = _selectedCell.Value;
                    HighlightCell(_selectedCell.Value.row, _selectedCell.Value.col, false);
                    _selectedCell = null;
                }
                else
                {
                    _dragSource = cell;
                }
                _isDragging = true;
            }
        }
    }

    private void Deselect()
    {
        if (_selectedCell.HasValue)
        {
            HighlightCell(_selectedCell.Value.row, _selectedCell.Value.col, false);
            _selectedCell = null;
        }
        if (_gameManager.State == GameState.Selecting)
            _gameManager.SetState(GameState.Idle);
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
            sr.color = on ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
        }
    }

    private IEnumerator ExecuteSwap(int r1, int c1, int r2, int c2)
    {
        yield return _swapHandler.SwapAndValidate(r1, c1, r2, c2);

        // After swap, state is Idle (valid) or Idle (invalid swapped back)
        if (_gameManager.State != GameState.Idle)
            _gameManager.SetState(GameState.Idle);

        // Run cascade (will be empty if no matches, which is fine)
        yield return _gameManager.cascadeManager.RunCascade();
    }
}
