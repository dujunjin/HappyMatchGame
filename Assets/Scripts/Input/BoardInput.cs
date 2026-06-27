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
        if (_gameManager.Flow == null) return;

        // Input gate: only during Idle/Selecting. Delegated to FlowController
        // so animation states (Swapping/Clearing/Falling/Refilling/GameOver)
        // uniformly reject ordinary input.
        if (!_gameManager.Flow.CanAcceptInput)
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
        // Suitcases are not player-movable: tapping one just clears any
        // current selection (you can only clear suitcases by matching next
        // to them or hitting them with a special).
        if (_board.Cells[cell.row, cell.col].elementType == ElementType.Suitcase)
        {
            Deselect();
            return;
        }
        if (_board.Cells[cell.row, cell.col].HasSpecial) return;

        if (_selectedCell == null)
        {
            _selectedCell = cell;
            _gameManager.SetState(GameState.Selecting);
            _gameManager.Audio?.Play(AudioCatalog.Event.UiClick);
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
                int sr = _dragSource.Value.row, sc = _dragSource.Value.col;
                int tr = target.row, tc = target.col;
                _isDragging = false;
                _dragSource = null;

                // Suitcases are immovable: never swap onto one.
                if (_board.Cells[tr, tc].elementType == ElementType.Suitcase)
                    return;

                // Special × special drag → combo; everything else → normal swap.
                if (_gameManager.comboHandler != null &&
                    _gameManager.comboHandler.IsCombo(sr, sc, tr, tc))
                {
                    StartCoroutine(ExecuteCombo(sr, sc, tr, tc));
                }
                else
                {
                    StartCoroutine(ExecuteSwap(sr, sc, tr, tc));
                }
            }
        }
        else if (!_isDragging && Input.GetMouseButton(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;
            var cell = _board.WorldToGrid(worldPos);

            // Allow dragging from any non-empty cell, INCLUDING specials, so the
            // player can drag a special onto an adjacent special to combo.
            // Suitcases are excluded — they are not player-movable.
            if (Helper.IsInBounds(cell.row, cell.col, _board.Rows, _board.Cols) &&
                !_board.Cells[cell.row, cell.col].IsEmpty &&
                _board.Cells[cell.row, cell.col].elementType != ElementType.Suitcase)
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
        if (_gameManager.Flow != null && _gameManager.Flow.State == GameState.Selecting)
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
        // Route the whole swap+cascade+dead-board-check through the presenter
        // so future phases can hook particles/sfx and the shuffle runs after
        // every cascade settles.
        if (_gameManager.boardPresenter != null)
            yield return _gameManager.boardPresenter.PresentSwap(r1, c1, r2, c2);
        else
            yield return _gameManager.cascadeManager.RunCascade();
    }

    private IEnumerator ExecuteCombo(int r1, int c1, int r2, int c2)
    {
        // Special×special combo: both specials are consumed by the combo
        // handler (no normal swap). The handler runs the combined clear, then
        // the presenter's cascade + dead-board check.
        if (_gameManager.comboHandler != null)
            yield return _gameManager.comboHandler.ActivateCombo(r1, c1, r2, c2);
        else if (_gameManager.boardPresenter != null)
            yield return _gameManager.boardPresenter.PresentCascade();
        else
            yield return _gameManager.cascadeManager.RunCascade();
    }
}
