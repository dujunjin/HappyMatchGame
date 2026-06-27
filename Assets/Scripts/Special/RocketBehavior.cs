using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Behavior for rocket special items - clears a full row or column when clicked.
/// </summary>
public class RocketBehavior : MonoBehaviour
{
    private BoardController _board;
    private GameManager _gameManager;
    private int _row;
    private int _col;
    private ElementType _elementType;
    private GameConfig.RocketDir _dir;
    private bool _activated;

    public void Init(BoardController board, GameManager gameManager, int row, int col, ElementType elementType, GameConfig.RocketDir dir)
    {
        _board = board;
        _gameManager = gameManager;
        _row = row;
        _col = col;
        _elementType = elementType;
        _dir = dir;
        _activated = false;
    }

    private void OnMouseUpAsButton()
    {
        if (_activated) return;
        // Input gate for specials: allow activation during Idle/Selecting/
        // Clearing (rockets can chain mid-cascade). Routed through
        // FlowController rather than reading GameManager.State directly.
        // OnMouseUpAsButton (not OnMouseDown) so dragging the rocket onto an
        // adjacent special to trigger a combo does not also fire its solo
        // effect on press.
        if (_gameManager.Flow == null || !_gameManager.Flow.CanActivateSpecial)
            return;
        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        _activated = true;
        _gameManager.SetState(GameState.Clearing);

        // Resolve the current grid position from the transform, since gravity
        // may have moved this item after it was created.
        var (row, col) = _board.WorldToGrid(transform.position);
        if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols))
        {
            _gameManager.SetState(GameState.Idle);
            yield break;
        }

        // Flash animation
        float flash = 0f;
        while (flash < 0.3f)
        {
            flash += Time.deltaTime;
            float t = flash / 0.3f;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.3f);
            yield return null;
        }

        // Determine which cells to clear (entire row/col, including the rocket's
        // own cell — the rocket is consumed when activated).
        var toClear = new List<(int, int)>();
        if (_dir == GameConfig.RocketDir.Horizontal)
        {
            for (int c = 0; c < _board.Cols; c++)
                toClear.Add((row, c));
        }
        else
        {
            for (int r = 0; r < _board.Rows; r++)
                toClear.Add((r, col));
        }

        // Check suitcases adjacent to cleared cells
        _gameManager.suitcaseManager.CheckAdjacentSuitcases(toClear);

        // Clear the line. The rocket's own gameObject is destroyed separately
        // below, so just reset its cell data here instead of calling DestroyCell
        // (which would try to destroy this gameObject early).
        foreach (var (r, c) in toClear)
        {
            if (r == row && c == col)
            {
                _board.Cells[r, c].elementType = ElementType.Empty;
                _board.Cells[r, c].specialType = GameConfig.SpecialType.None;
                _board.Cells[r, c].gameObject = null;
            }
            else
            {
                _board.DestroyCell(r, c);
            }
        }

        // Shrink out.
        yield return AnimationHelper.TweenScale(transform, Vector3.one, Vector3.zero, 0.15f);

        // Start the cascade via the presenter so the dead-board check (and
        // future presentation hooks) runs after the rocket's line clears.
        if (_gameManager.boardPresenter != null)
            _gameManager.StartCoroutine(_gameManager.boardPresenter.PresentCascade());
        else
            _gameManager.StartCoroutine(_gameManager.cascadeManager.RunCascade());
        Object.Destroy(gameObject);
    }
}
