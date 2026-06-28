using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Propeller special item. On activation (tap or combo) the propeller takes
/// off, flies along an arc to the highest-priority remaining suitcase (topmost
/// / leftmost), and clears the target plus its 4-neighbors. If no suitcase
/// remains it picks a random normal element instead. Cleared suitcases are
/// routed through TargetPresentation so they count toward the goal.
///
/// Activation model: OnMouseUpAsButton (tap = activate). This avoids firing
/// when the player drags from the propeller onto an adjacent special to
/// trigger a rocket×propeller combo (handled by BoardInput + SpecialComboHandler).
/// </summary>
public class PropellerBehavior : MonoBehaviour
{
    private BoardController _board;
    private GameManager _gameManager;
    private int _row;
    private int _col;
    private ElementType _elementType;
    private bool _activated;

    private float _idleSpin;

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
        if (_gameManager.Flow == null || !_gameManager.Flow.CanActivateSpecial) return;
        StartCoroutine(Activate());
    }

    private void Update()
    {
        // Idle rotor spin so the propeller reads as "live" while waiting.
        // Stop once activated: the flight coroutine drives rotation during the
        // arc, and the gameObject is destroyed at the end.
        if (_activated) return;
        _idleSpin += Time.deltaTime * 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, -_idleSpin);
    }

    private IEnumerator Activate()
    {
        _activated = true;
        _gameManager.SetState(GameState.Clearing);

        // Resolve current grid position (gravity may have moved it).
        var (row, col) = _board.WorldToGrid(transform.position);
        if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols))
        {
            _gameManager.SetState(GameState.Idle);
            yield break;
        }

        // Detach the propeller from its cell: clear the cell's data but keep
        // this gameObject alive to fly. Gravity may refill the cell during
        // the flight, which is fine.
        _board.Cells[row, col].elementType = ElementType.Empty;
        _board.Cells[row, col].specialType = GameConfig.SpecialType.None;
        _board.Cells[row, col].gameObject = null;

        // Takeoff: brief scale-up, tilt and lift before the homing arc.
        Vector3 start = transform.position;
        float takeoff = 0f;
        const float takeoffDuration = 0.16f;
        while (takeoff < takeoffDuration)
        {
            takeoff += Time.deltaTime;
            float t = PolishMotion.EaseOutBack(takeoff / takeoffDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.25f, t);
            transform.position = start + Vector3.up * (0.16f * Mathf.Clamp01(t));
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -18f, Mathf.Clamp01(t)));
            yield return null;
        }
        start = transform.position;

        // Choose target.
        var suitcases = _board.GetSuitcaseCells();
        Vector3 targetPos;
        int targetRow, targetCol;
        if (suitcases.Count > 0)
        {
            (targetRow, targetCol) = suitcases[0]; // topmost-leftmost
        }
        else
        {
            // No suitcase left: pick a random non-empty, non-special cell.
            var normals = new List<(int, int)>();
            for (int r = 0; r < _board.Rows; r++)
                for (int c = 0; c < _board.Cols; c++)
                {
                    var cell = _board.Cells[r, c];
                    if (!cell.IsEmpty && !cell.HasSpecial && cell.elementType != ElementType.Suitcase)
                        normals.Add((r, c));
                }
            if (normals.Count == 0)
            {
                // Board completely empty of hittable targets: just finish.
                yield return AnimationHelper.ShrinkAndDestroy(gameObject, 0.15f);
                _gameManager.StartCoroutine(_gameManager.boardPresenter != null
                    ? _gameManager.boardPresenter.PresentCascade()
                    : _gameManager.cascadeManager.RunCascade());
                yield break;
            }
            (targetRow, targetCol) = normals[Random.Range(0, normals.Count)];
        }
        targetPos = _board.GetWorldPosition(targetRow, targetCol);

        // Flight duration scales with distance (0.45–0.70s per spec).
        float distance = Vector3.Distance(start, targetPos);
        float flightDuration = Mathf.Clamp(0.45f + distance * 0.06f, 0.45f, 0.70f);

        // Quadratic bezier arc: control point above the midpoint.
        Vector3 mid = (start + targetPos) * 0.5f;
        float arcHeight = 0.6f + distance * 0.25f;
        Vector3 control = new Vector3(mid.x, mid.y + arcHeight, mid.z);

        // Phase E: propeller fly SFX during flight.
        _gameManager.Audio?.Play(AudioCatalog.Event.PropellerFly);

        yield return FlyArc(start, control, targetPos, flightDuration);

        // Phase E: propeller fly SFX plays during flight (started at takeoff).
        // Phase E: starburst at the impact point + hit SFX.
        if (_gameManager.Vfx != null)
            _gameManager.Vfx.SpawnPropellerHit(targetPos);
        _gameManager.Audio?.Play(AudioCatalog.Event.PropellerHit);

        // Impact: clear target + 4-neighbors, count suitcases.
        var clearCells = new List<(int, int)> { (targetRow, targetCol) };
        AddIfValid(clearCells, targetRow - 1, targetCol);
        AddIfValid(clearCells, targetRow + 1, targetCol);
        AddIfValid(clearCells, targetRow, targetCol - 1);
        AddIfValid(clearCells, targetRow, targetCol + 1);

        var hitSuitcases = new List<(int, int)>();
        foreach (var (r, c) in clearCells)
        {
            if (_board.Cells[r, c].elementType == ElementType.Suitcase)
                hitSuitcases.Add((r, c));
        }

        // Route suitcase hits BEFORE destroying the cells so their gameObjects
        // are still around to become flyers (otherwise the display counter
        // desyncs from the logical counter).
        if (hitSuitcases.Count > 0 && _gameManager.targetPresentation != null)
            _gameManager.targetPresentation.OnSuitcaseHit(hitSuitcases.Count, hitSuitcases);

        // Brief impact flash on the cells being cleared.
        yield return FlashCells(clearCells);

        foreach (var (r, c) in clearCells)
            _board.DestroyCell(r, c);

        // Shrink out the propeller.
        yield return AnimationHelper.ShrinkAndDestroy(gameObject, 0.15f);

        // Run cascade via the presenter so gravity/refill + dead-board check fire.
        if (_gameManager.boardPresenter != null)
            _gameManager.StartCoroutine(_gameManager.boardPresenter.PresentCascade());
        else
            _gameManager.StartCoroutine(_gameManager.cascadeManager.RunCascade());
    }

    private void AddIfValid(List<(int, int)> list, int r, int c)
    {
        if (Helper.IsInBounds(r, c, _board.Rows, _board.Cols))
            list.Add((r, c));
    }

    private IEnumerator FlyArc(Vector3 p0, Vector3 p1, Vector3 p2, float duration)
    {
        float elapsed = 0f;
        float trailAccum = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Quadratic bezier.
            float u = 1f - t;
            Vector3 pos = u * u * p0 + 2f * u * t * p1 + t * t * p2;
            transform.position = pos;
            // Keep spinning during flight.
            _idleSpin += Time.deltaTime * 540f;
            transform.rotation = Quaternion.Euler(0f, 0f, -_idleSpin);

            // Phase E: drop a short gold trail particle every ~0.04s.
            trailAccum += Time.deltaTime;
            if (trailAccum >= 0.04f && _gameManager != null && _gameManager.Vfx != null)
            {
                trailAccum = 0f;
                _gameManager.Vfx.SpawnPropellerTrail(pos);
            }
            yield return null;
        }
        transform.position = p2;
    }

    private IEnumerator FlashCells(List<(int, int)> cells)
    {
        float duration = 0.14f;
        var items = new List<SpriteRenderer>();
        foreach (var (r, c) in cells)
        {
            if (_board.Cells[r, c].gameObject == null) continue;
            var sr = _board.Cells[r, c].gameObject.GetComponent<SpriteRenderer>();
            if (sr != null) items.Add(sr);
        }
        if (items.Count == 0) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].transform.localScale = Vector3.one * s;
                    items[i].color = new Color(1f, 1f, 1f, 1f);
                }
            }
            yield return null;
        }
    }
}
