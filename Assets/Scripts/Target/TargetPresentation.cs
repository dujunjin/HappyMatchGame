using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Routes suitcase-clear events into the target counter. In Phase A this is a
/// thin stub: it just forwards the decrement to GameFlowController (preserving
/// the previous behavior). Phase C will replace OnSuitcaseHit with a flying
/// animation toward the target bar, a bounce on the target slot, and a
/// logical/display/in-flight triple-counter sync, plus a victory barrier.
///
/// Keeping this indirection now means SuitcaseManager/specials do not call
/// DecreaseSuitcase directly, so Phase C can intercept without touching them.
/// </summary>
public class TargetPresentation
{
    private GameFlowController _flow;
    private GameManager _gameManager;

    public void Init(GameManager gameManager, GameFlowController flow)
    {
        _gameManager = gameManager;
        _flow = flow;
    }

    /// <summary>
    /// Called when one or more suitcases are cleared (adjacent to a match,
    /// caught in a rocket line / bomb blast, etc.). Phase A: straight to the
    /// counter. Phase C: spawn a bezier flyer per suitcase and decrement the
    /// display counter on arrival.
    /// </summary>
    public void OnSuitcaseHit(int count, List<(int row, int col)> cells)
    {
        if (_flow == null) return;
        _flow.DecreaseSuitcase(count);
    }

    /// <summary>
    /// Called when the remaining-suitcase counter reaches zero. Phase A: the
    /// GameFlowController already shows the win dialog. Phase C will add the
    /// victory barrier / Great text / big suitcase burst (Phase D).
    /// </summary>
    public void OnTargetReached() { }
}
