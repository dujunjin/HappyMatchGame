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

    private void OnMouseUpAsButton()
    {
        if (_activated) return;
        // Input gate for specials: allow activation during Idle/Selecting/
        // Clearing (bombs can chain mid-cascade). Routed through FlowController.
        // OnMouseUpAsButton so dragging the bomb onto an adjacent special for a
        // combo does not also fire its solo blast on press.
        if (_gameManager.Flow == null || !_gameManager.Flow.CanActivateSpecial)
            return;
        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        _activated = true;
        _gameManager.SetState(GameState.Clearing);
        _gameManager.Audio?.Play(AudioCatalog.Event.BombActivate);

        // Resolve the current grid position from the transform, since gravity
        // may have moved this item after it was created.
        var (row, col) = _board.WorldToGrid(transform.position);
        if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols))
        {
            _gameManager.SetState(GameState.Idle);
            yield break;
        }

        // Compress into a charged core, then overshoot into the blast.
        float pulse = 0f;
        const float chargeDuration = 0.18f;
        while (pulse < chargeDuration)
        {
            pulse += Time.deltaTime;
            float t = Mathf.Clamp01(pulse / chargeDuration);
            float s = t < 0.62f
                ? Mathf.Lerp(1f, 0.78f, PolishMotion.EaseInOutCubic(t / 0.62f))
                : Mathf.Lerp(0.78f, 1.42f, PolishMotion.EaseOutBack((t - 0.62f) / 0.38f));
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

        // Phase E: shock ring + sparks + screen shake at the blast center.
        if (_gameManager.Vfx != null)
            _gameManager.Vfx.SpawnBombBlast(_board.GetWorldPosition(row, col));

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
        yield return AnimationHelper.TweenScale(transform, transform.localScale, Vector3.zero, 0.15f);

        // Start the cascade via the presenter so the dead-board check (and
        // future presentation hooks) runs after the bomb's blast clears.
        if (_gameManager.boardPresenter != null)
            _gameManager.StartCoroutine(_gameManager.boardPresenter.PresentCascade());
        else
            _gameManager.StartCoroutine(_gameManager.cascadeManager.RunCascade());
        Object.Destroy(gameObject);
    }
}
