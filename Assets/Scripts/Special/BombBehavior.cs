using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Behavior for bomb special items - clears a 3x3 area when clicked.
/// </summary>
public class BombBehavior : MonoBehaviour
{
    private BoardController _board;
    private GameManager _gameManager;
    private int _row;
    private int _col;
    private ElementType _elementType;
    private bool _activated;

    public void Init(BoardController board, GameManager gameManager, int row, int col, ElementType elementType)
    {
        _board = board;
        _gameManager = gameManager;
        _row = row;
        _col = col;
        _elementType = elementType;
        _activated = false;
    }

    private void OnMouseDown()
    {
        if (_activated) return;
        GameState state = _gameManager.State;
        if (state != GameState.Idle && state != GameState.Selecting && state != GameState.Clearing)
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

        // Pulse animation
        float pulse = 0f;
        while (pulse < 0.25f)
        {
            pulse += Time.deltaTime;
            float s = 1f + Mathf.Sin(pulse / 0.25f * Mathf.PI) * 0.4f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        // Clear 3x3 area (including the bomb's own cell — it is consumed).
        var toClear = new List<(int, int)>();
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                int r = row + dr;
                int c = col + dc;
                if (Helper.IsInBounds(r, c, _board.Rows, _board.Cols))
                    toClear.Add((r, c));
            }
        }

        // Check suitcases
        _gameManager.suitcaseManager.CheckAdjacentSuitcases(toClear);

        // Clear the area. The bomb's own gameObject is destroyed separately
        // below, so just reset its cell data here instead of calling DestroyCell.
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

        // Start the cascade on the GameManager so it survives this gameObject
        // being destroyed (the bomb's own coroutine would otherwise die with it).
        _gameManager.StartCoroutine(_gameManager.cascadeManager.RunCascade());
        Object.Destroy(gameObject);
    }
}
