using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase C: target-collection presentation. Owns the DISPLAY and FLYING
/// counters (the LOGICAL counter stays in GameFlowController).
///
/// When a suitcase is hit (OnSuitcaseHit), the suitcase's gameObject is
/// detached from its cell immediately (cell marked Empty so it can't be hit
/// again / gravity can refill), then plays a brief hit animation (flash +
/// compress), then flies along a quadratic bezier arc to the target bar
/// (0.50–0.65s). On arrival the flyer is destroyed, the target bar bounces,
/// the DISPLAY counter ticks down, and once the last flyer lands the settle
/// flow runs (which resolves the pending win/lose — the victory barrier).
/// </summary>
public class TargetPresentation
{
    private GameManager _gameManager;
    private GameFlowController _flow;
    private BoardController _board;

    /// <summary>Count shown in the TopBar (lags the logical count).</summary>
    public int displayCount { get; private set; }

    /// <summary>Flyers currently in flight.</summary>
    public int flyingCount { get; private set; }

    public bool HasInFlight => flyingCount > 0;

    public void Init(GameManager gameManager, GameFlowController flow)
    {
        _gameManager = gameManager;
        _flow = flow;
        _board = gameManager.boardController;
        displayCount = flow.RemainingSuitcases;
        flyingCount = 0;
        _gameManager.RefreshTopBar();
    }

    /// <summary>
    /// Called when one or more suitcases are cleared (adjacent to a match, or
    /// part of a 3-suitcase line, or hit by a special/combo). Detaches each
    /// suitcase's gameObject (logical clear, immediate) and launches a
    /// hit-and-fly coroutine; decrements the logical counter right away.
    /// </summary>
    public void OnSuitcaseHit(int count, List<(int row, int col)> cells)
    {
        if (_flow == null || _board == null) return;

        // Logical decrement immediately (sets WinPending if the goal is met).
        _flow.DecreaseSuitcase(count);

        // Phase E: suitcase break SFX.
        _gameManager.Audio?.Play(AudioCatalog.Event.SuitcaseHit);

        int launchIndex = 0;
        foreach (var (row, col) in cells)
        {
            if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols)) continue;
            var cell = _board.Cells[row, col];
            if (cell.elementType != ElementType.Suitcase) continue;

            GameObject go = cell.gameObject;
            // Detach immediately: cell is now logically empty (no double-hit,
            // gravity may refill it) while the gameObject flies off.
            _board.Cells[row, col].elementType = ElementType.Empty;
            _board.Cells[row, col].specialType = GameConfig.SpecialType.None;
            _board.Cells[row, col].gameObject = null;

            if (go == null)
            {
                // No gameObject to fly (e.g. cell was pre-destroyed): decrement
                // the display counter directly so it stays in sync with the
                // logical counter and the TopBar reaches 0 at the win.
                displayCount = displayCount > 0 ? displayCount - 1 : 0;
                _gameManager.RefreshTopBar();
                continue;
            }

            flyingCount++;
            _gameManager.StartCoroutine(HitAndFly(go, go.transform.position, launchIndex * 0.035f));
            launchIndex++;
        }
    }

    /// <summary>
    /// Hit animation (flash + compress) then bezier flight to the target bar.
    /// On arrival: destroy flyer, bounce target, tick the display counter
    /// down, and on the last flyer run the settle flow.
    /// </summary>
    private IEnumerator HitAndFly(GameObject go, Vector3 startPos, float launchDelay)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 5; // render above the board

        // A slight stagger keeps multi-target collections readable and musical.
        if (launchDelay > 0f) yield return new WaitForSeconds(launchDelay);

        // --- Hit: flash white + compress, ~0.15s ---
        const float hitDur = 0.15f;
        float elapsed = 0f;
        Vector3 baseScale = go.transform.localScale;
        while (elapsed < hitDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitDur);
            // Compress to 0.8 and back via a sine.
            float s = 1f - 0.22f * Mathf.Sin(t * Mathf.PI);
            go.transform.localScale = baseScale * s;
            // Flash toward white at the start, ease back to normal.
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.8f), t);
            yield return null;
        }
        if (sr != null) sr.color = Color.white;
        go.transform.localScale = baseScale;

        // --- Fly: quadratic bezier arc to the target bar, 0.50–0.65s ---
        Vector3 target = _gameManager.gameUI != null
            ? _gameManager.gameUI.GetTargetWorldPosition()
            : startPos + Vector3.up * 3f;
        float dist = Vector3.Distance(startPos, target);
        float flyDur = Mathf.Clamp(0.50f + dist * 0.03f, 0.50f, 0.65f);
        Vector3 mid = (startPos + target) * 0.5f;
        float arcHeight = 0.8f + dist * 0.18f;
        Vector3 control = new Vector3(mid.x, mid.y + arcHeight, mid.z);

        elapsed = 0f;
        while (elapsed < flyDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDur);
            float eased = PolishMotion.EaseInOutCubic(t);
            float u = 1f - t;
            Vector3 position = u * u * startPos + 2f * u * t * control + t * t * target;
            // Blend the bezier's time with an eased homing finish.
            go.transform.position = Vector3.Lerp(position, target, Mathf.Clamp01((eased - 0.82f) / 0.18f) * 0.16f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(t * Mathf.PI) * 135f);
            float depthScale = Mathf.Lerp(1f, 0.76f, Mathf.Sin(t * Mathf.PI));
            float arrivalPop = t > 0.82f ? Mathf.Lerp(1f, 1.18f, (t - 0.82f) / 0.18f) : 1f;
            go.transform.localScale = baseScale * depthScale * arrivalPop;
            yield return null;
        }

        // --- Arrive ---
        if (_gameManager.Vfx != null) _gameManager.Vfx.SpawnTargetArrival(target);
        Object.Destroy(go);
        if (_gameManager.gameUI != null) _gameManager.gameUI.BounceTarget();

        // Phase E: collect chime + target-bounce pop.
        _gameManager.Audio?.Play(AudioCatalog.Event.SuitcaseCollect);
        _gameManager.Audio?.Play(AudioCatalog.Event.TargetBounce);

        flyingCount = flyingCount > 0 ? flyingCount - 1 : 0;
        displayCount = displayCount > 0 ? displayCount - 1 : 0;
        _gameManager.RefreshTopBar();

        // Only the cascade-settle path (BoardPresenter sets Settling after the
        // cascade ends) should resolve end-game here. If a flyer lands while
        // the cascade is still running (Clearing/Falling/Refilling), just
        // tick the counters — BoardPresenter.PresentCascade will call
        // OnSettleComplete once the cascade finishes (either directly if no
        // flyers remain, or after the last Settling-period arrival).
        if (flyingCount <= 0 && _flow != null && _flow.State == GameState.Settling)
        {
            _gameManager.OnSettleComplete();
        }
    }
}
