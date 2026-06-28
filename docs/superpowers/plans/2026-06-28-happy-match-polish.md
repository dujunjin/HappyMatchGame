# Happy Match Animation and Page Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve the working 9×8 match-3 game while making swaps, clears, falls, targets, specials, UI, background, and victory presentation feel materially smoother and more refined.

**Architecture:** Keep the existing rule/data classes authoritative and add a small deterministic presentation-math layer that all animation coroutines share. Improve visuals in the existing `BoardController`, `TopBarView`, environment, VFX, target, and special-effect boundaries; use one original portrait background asset and retain procedural fallbacks. Verification combines Unity EditMode tests, a compile-only batch pass, portrait runtime captures, interaction checks, and an updated acceptance report.

**Tech Stack:** Unity 2022.3.62f3c1, C# 9, Unity Test Framework 1.1.33, uGUI/TextMesh Pro, SpriteRenderer, procedural audio/VFX, built-in image generation for one project-bound raster background.

---

## File map

- Create `Assets/Scripts/Presentation/PolishMotion.cs`: pure easing/duration/matchability helpers.
- Create `Assets/Scripts/Presentation/PieceVisual.cs`: per-piece shadow, glow, selection, and settle feedback.
- Create `Assets/Tests/EditMode/PolishMotionTests.cs`: deterministic motion and matchability tests.
- Create `Assets/Tests/EditMode/PieceVisualTests.cs`: board-piece hierarchy and selection-state tests.
- Modify `Assets/Scripts/Utils/AnimationHelper.cs`: simultaneous movement, bounce settle, and punch-scale routines.
- Modify `Assets/Scripts/Gameplay/SwapHandler.cs`: simultaneous exchange and stronger invalid-swap recoil.
- Modify `Assets/Scripts/Gameplay/MatchDetector.cs`: suitcases are obstacles/targets, never color matches.
- Modify `Assets/Scripts/Gameplay/CascadeManager.cs`: pop/flash clear and distance-aware fall/refill settle.
- Modify `Assets/Scripts/Board/BoardController.cs`: polished blue glass cells, piece shadows, consistent special scaling.
- Modify `Assets/Scripts/UI/TopBarView.cs`: warm target pill, refined steps badge, safe responsive spacing, richer counter bounce.
- Modify `Assets/Scripts/Core/GameManager.cs`: polished board backdrop and background/theme wiring.
- Modify `Assets/Scripts/Environment/ChristmasBackground.cs`: layered parallax/ambient drift on top of the portrait background.
- Modify `Assets/Scripts/Environment/SnowField.cs`: front/back depth layers and rain streak accents.
- Modify `Assets/Scripts/Vfx/VfxSystem.cs`: stronger yet bounded clear, rocket, bomb, propeller, and target-arrival accents.
- Modify `Assets/Scripts/Special/RocketBehavior.cs`, `BombBehavior.cs`, `PropellerBehavior.cs`: anticipation, impact, and settle timing.
- Modify `Assets/Scripts/Target/TargetPresentation.cs`: curved scale/spin flight, arrival halo, and staggered multi-target rhythm.
- Modify `Assets/Scripts/UI/WinSequence.cs`: target completion beat, board exit polish, and legible portrait finale.
- Create `Assets/Sprites/Background_ChristmasNight_Polished.png` and `.meta`: original portrait winter-night game background.
- Modify `Assets/ChristmasTheme.asset` and `Assets/Scenes/SampleScene.unity`: reference the polished background and stable presentation settings.
- Modify `Document/ASSET_LICENSES.md`: identify the generated original asset and retained bundled assets.
- Modify `Document/ACCEPTANCE_CHECKLIST.md`: record fresh automated and visual evidence.

## Task 1: Lock down presentation math and obstacle correctness

**Files:**
- Create: `Assets/Tests/EditMode/PolishMotionTests.cs`
- Create: `Assets/Scripts/Presentation/PolishMotion.cs`
- Modify: `Assets/Scripts/Gameplay/MatchDetector.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using NUnit.Framework;

public class PolishMotionTests
{
    [Test] public void EaseOutBack_ClampsEndpoints()
    {
        Assert.AreEqual(0f, PolishMotion.EaseOutBack(0f), 0.0001f);
        Assert.AreEqual(1f, PolishMotion.EaseOutBack(1f), 0.0001f);
    }

    [Test] public void FallDuration_GrowsWithDistanceAndStaysBounded()
    {
        Assert.Less(PolishMotion.FallDuration(1), PolishMotion.FallDuration(6));
        Assert.That(PolishMotion.FallDuration(20), Is.InRange(0.18f, 0.34f));
    }

    [TestCase(ElementType.Red, true)]
    [TestCase(ElementType.Green, true)]
    [TestCase(ElementType.Suitcase, false)]
    [TestCase(ElementType.Empty, false)]
    public void IsColorMatchable_OnlyNormalElements(ElementType type, bool expected)
    {
        Assert.AreEqual(expected, PolishMotion.IsColorMatchable(type));
    }
}
```

- [ ] **Step 2: Run EditMode tests and verify RED**

Run:

```bash
Unity -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/hm-polish-red.xml -logFile /tmp/hm-polish-red.log
```

Expected: compilation/test failure because `PolishMotion` does not exist.

- [ ] **Step 3: Add the minimal pure presentation helper**

```csharp
public static class PolishMotion
{
    public static float EaseOutBack(float t)
    {
        t = UnityEngine.Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * UnityEngine.Mathf.Pow(t - 1f, 3f) + c1 * UnityEngine.Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseInOutCubic(float t)
    {
        t = UnityEngine.Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - UnityEngine.Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    public static float FallDuration(int rowDistance) => UnityEngine.Mathf.Clamp(0.18f + UnityEngine.Mathf.Max(0, rowDistance - 1) * 0.025f, 0.18f, 0.34f);

    public static bool IsColorMatchable(ElementType type) =>
        type == ElementType.Red || type == ElementType.Blue || type == ElementType.Yellow || type == ElementType.Green;
}
```

Use `PolishMotion.IsColorMatchable(current)` in horizontal and vertical scans so suitcases never form matches.

- [ ] **Step 4: Run EditMode tests and verify GREEN**

Expected: all baseline tests plus `PolishMotionTests` pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation Assets/Tests/EditMode/PolishMotionTests.cs Assets/Scripts/Gameplay/MatchDetector.cs
git commit -m "test: lock polish motion and obstacle rules"
```

## Task 2: Make swap, clear, fall, and refill motion feel responsive

**Files:**
- Modify: `Assets/Scripts/Utils/AnimationHelper.cs`
- Modify: `Assets/Scripts/Gameplay/SwapHandler.cs`
- Modify: `Assets/Scripts/Gameplay/CascadeManager.cs`
- Modify: `Assets/Scripts/Core/GameConfig.cs`

- [ ] **Step 1: Add failing assertions for timing constants and distance behavior**

Extend `PolishMotionTests`:

```csharp
[Test] public void SwapTiming_IsSnappy()
{
    Assert.That(GameConfig.SwapDuration, Is.InRange(0.14f, 0.18f));
    Assert.Less(GameConfig.SwapDuration, GameConfig.ClearDuration + 0.01f);
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Expected: current `SwapDuration == 0.2f` fails the upper bound.

- [ ] **Step 3: Implement simultaneous motion primitives**

Add to `AnimationHelper`:

```csharp
public static IEnumerator TweenPositionsJuicy(List<(Transform tr, Vector3 from, Vector3 to, int distance)> movers)
{
    float duration = 0f;
    foreach (var m in movers) duration = Mathf.Max(duration, PolishMotion.FallDuration(m.distance));
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        foreach (var m in movers)
        {
            if (m.tr == null) continue;
            float local = Mathf.Clamp01(elapsed / PolishMotion.FallDuration(m.distance));
            m.tr.position = Vector3.LerpUnclamped(m.from, m.to, PolishMotion.EaseOutBack(local));
        }
        yield return null;
    }
    foreach (var m in movers) if (m.tr != null) { m.tr.position = m.to; m.tr.localScale = Vector3.one; }
}
```

Use a single parallel mover list for both swap directions. Use a short 0.04 second hold and a 1.08 scale recoil before invalid swaps return. Clear animation follows `1.0 → 1.14 → 0` with a white flash; fall/refill duration uses row distance and ends with a small squash/settle.

- [ ] **Step 4: Run all EditMode tests and compile in batch mode**

Expected: tests pass and the log contains no C# compilation errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/GameConfig.cs Assets/Scripts/Utils/AnimationHelper.cs Assets/Scripts/Gameplay/SwapHandler.cs Assets/Scripts/Gameplay/CascadeManager.cs Assets/Tests/EditMode/PolishMotionTests.cs
git commit -m "feat: add responsive core match animations"
```

## Task 3: Refine board cells, pieces, and selection feedback

**Files:**
- Create: `Assets/Tests/EditMode/PieceVisualTests.cs`
- Create: `Assets/Scripts/Presentation/PieceVisual.cs`
- Modify: `Assets/Scripts/Board/BoardController.cs`
- Modify: `Assets/Scripts/Gameplay/SwapHandler.cs`
- Modify: `Assets/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Write a failing component-level test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class PieceVisualTests
{
    [Test]
    public void Configure_CreatesShadowAndSelectionHalo()
    {
        var go = new GameObject("Piece");
        go.AddComponent<SpriteRenderer>();
        var visual = go.AddComponent<PieceVisual>();
        visual.Configure();
        Assert.IsNotNull(go.transform.Find("SoftShadow"));
        Assert.IsNotNull(go.transform.Find("SelectionHalo"));
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run the test and verify RED**

Expected: failure because `PieceVisual` is missing.

- [ ] **Step 3: Implement per-piece visual hierarchy**

`PieceVisual.Configure()` creates a dark translucent shadow at `(0.025,-0.04,0)`, a disabled cyan-white halo behind the piece, and caches the main renderer. `SetSelected(true)` enables the halo and animates to 1.07 scale; `SetSelected(false)` restores scale/color. `SetSprite(Sprite)` updates main and shadow sprites.

Route every cell and refill object through `PieceVisual`. Replace `SwapHandler.HighlightCell` alpha dimming with `PieceVisual.SetSelected(on)`. Change cell glass from neutral gray to blue-tinted transparent fill with a restrained edge highlight, and soften the board backdrop so the background remains visible.

- [ ] **Step 4: Run EditMode tests and capture a portrait board frame**

Expected: tests pass; pieces read larger and clearer, shadows remain attached during swaps/falls, and the board no longer looks like a gray spreadsheet.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Presentation/PieceVisual.cs Assets/Tests/EditMode/PieceVisualTests.cs Assets/Scripts/Board/BoardController.cs Assets/Scripts/Gameplay/SwapHandler.cs Assets/Scripts/Core/GameManager.cs
git commit -m "feat: polish board cells and piece feedback"
```

## Task 4: Upgrade the portrait background and environmental depth

**Files:**
- Create: `Assets/Sprites/Background_ChristmasNight_Polished.png`
- Create: `Assets/Sprites/Background_ChristmasNight_Polished.png.meta`
- Modify: `Assets/ChristmasTheme.asset`
- Modify: `Assets/Scenes/SampleScene.unity`
- Modify: `Assets/Scripts/Environment/ChristmasBackground.cs`
- Modify: `Assets/Scripts/Environment/SnowField.cs`
- Modify: `Document/ASSET_LICENSES.md`

- [ ] **Step 1: Generate one original project background**

Use built-in image generation with this production prompt:

```text
Use case: stylized-concept
Asset type: portrait mobile match-3 game background, board overlays the middle
Primary request: an original richly painted winter Christmas night scene, whimsical premium casual-game art
Scene/backdrop: deep navy town alley, warm cabin window on the left, glowing decorated fir tree and small gifts on the right, moonlight, snow-covered ground, subtle distant pine silhouettes
Composition/framing: portrait approximately 820:1022; quiet low-contrast negative space through the center and upper-middle for a game board and target bar; detail concentrated at the edges and bottom
Lighting/mood: cold blue moonlight balanced by warm amber practical lights, cozy but dramatic
Materials/textures: soft painterly snow, frosted wood, atmospheric depth, gentle bloom
Constraints: no text, no logos, no watermark, no UI, no game board, no copyrighted characters, no photoreal people
Avoid: flat vector shapes, clutter behind the board, excessive saturation, pure black regions
```

Copy the selected output into `Assets/Sprites/Background_ChristmasNight_Polished.png`, crop/resize non-destructively to the exact portrait ratio, and keep the existing background as fallback.

- [ ] **Step 2: Wire and deepen the environment**

Assign the new sprite to `ChristmasTheme.asset` and the scene `ChristmasBackground`. Add two low-amplitude parallax overlays and a soft moon-glow drift without moving the base image. Split snow into slow background flakes, medium flakes, and a few fast foreground streaks; cap live objects and reuse cached sprites.

- [ ] **Step 3: Reimport and verify portrait coverage**

Run Unity batch import/compile. Expected: no seams at 390×844 and 820×1022; board remains the visual focus.

- [ ] **Step 4: Update the license manifest and commit**

Record the asset as an original AI-assisted project asset with generation date and project path, then:

```bash
git add Assets/Sprites/Background_ChristmasNight_Polished.png* Assets/ChristmasTheme.asset Assets/Scenes/SampleScene.unity Assets/Scripts/Environment/ChristmasBackground.cs Assets/Scripts/Environment/SnowField.cs Document/ASSET_LICENSES.md
git commit -m "feat: add polished winter night environment"
```

## Task 5: Polish target bar, collection flight, specials, and victory beats

**Files:**
- Modify: `Assets/Scripts/UI/TopBarView.cs`
- Modify: `Assets/Scripts/Target/TargetPresentation.cs`
- Modify: `Assets/Scripts/Vfx/VfxSystem.cs`
- Modify: `Assets/Scripts/Special/RocketBehavior.cs`
- Modify: `Assets/Scripts/Special/BombBehavior.cs`
- Modify: `Assets/Scripts/Special/PropellerBehavior.cs`
- Modify: `Assets/Scripts/UI/WinSequence.cs`
- Modify: `Assets/Tests/EditMode/GameUITests.cs`

- [ ] **Step 1: Extend UI tests before changing production code**

Add assertions that initialized `TopBarView` contains `TargetPill`, `StepsBadge`, and accessible labels; run and verify the test fails against the current names/layout.

- [ ] **Step 2: Refine the top bar and target arrival**

Create a warm cream target pill nested inside a restrained glass header, keep the small steps badge visually secondary, use Chinese-independent icon/count hierarchy, and keep safe margins at portrait widths. Target flyers follow eased Bezier motion, shrink slightly in depth, grow on arrival, emit an arrival ring, and stagger simultaneous arrivals by 0.035 seconds.

- [ ] **Step 3: Add special anticipation and impact**

Rocket: 0.12 second compression/flash, dual-direction beam head, and per-cell hit cadence. Bomb: 0.18 second squash/charge, expanding shock ring, and camera impulse. Propeller: 0.16 second lift with tilt, curved trail, target hover beat, then impact. Preserve current clear sets and cascade routing.

- [ ] **Step 4: Tighten the win sequence**

Wait for target arrival, bounce the completed pill, ease board down with a soft vignette, pop `Great!` with readable outline/shadow, then stage the chest burst within the portrait safe region. Ensure replay cleans every spawned object and restores camera/board transforms.

- [ ] **Step 5: Run tests and commit**

```bash
git add Assets/Scripts/UI/TopBarView.cs Assets/Scripts/Target/TargetPresentation.cs Assets/Scripts/Vfx/VfxSystem.cs Assets/Scripts/Special Assets/Scripts/UI/WinSequence.cs Assets/Tests/EditMode/GameUITests.cs
git commit -m "feat: polish target specials and victory presentation"
```

## Task 6: Full self-acceptance and delivery evidence

**Files:**
- Modify: `Document/ACCEPTANCE_CHECKLIST.md`
- Create: `Document/ACCEPTANCE_REPORT_2026-06-28.md`
- Create: `Artifacts/Acceptance/initial.png`
- Create: `Artifacts/Acceptance/specials.png`
- Create: `Artifacts/Acceptance/target-flight.png`
- Create: `Artifacts/Acceptance/victory.png`
- Create: `Artifacts/Acceptance/happy-match-polish-demo.mp4`

- [ ] **Step 1: Run all EditMode tests**

```bash
Unity -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults Artifacts/Acceptance/editmode-results.xml -logFile Artifacts/Acceptance/editmode.log
```

Expected: all tests pass, zero failures/skips.

- [ ] **Step 2: Run a clean compile/import pass**

```bash
Unity -batchmode -nographics -projectPath "$PWD" -logFile Artifacts/Acceptance/compile.log -quit
```

Expected: exit code 0; no `error CS`, unhandled exception, missing script, or missing reference.

- [ ] **Step 3: Perform portrait interaction acceptance**

At 820×1022, verify selection, valid swap, invalid swap, ordinary match, cascade, target collection, rocket, bomb, propeller, input locking, failure restart, and victory. Capture the four named PNGs and record one complete demo MP4 with audible ambience/SFX.

- [ ] **Step 4: Compare with reference and write the report**

The report maps every item in `Document/Demo 开发计划书.md` and the approved design to direct evidence. It explicitly records any remaining cosmetic deviation; no broad success claim is allowed from compile/tests alone.

- [ ] **Step 5: Final verification and commit**

Run `git diff --check`, inspect `git status`, parse the XML result counts, scan fresh logs for errors, inspect every acceptance image, and probe the MP4 for video plus AAC audio. Then:

```bash
git add Document/ACCEPTANCE_CHECKLIST.md Document/ACCEPTANCE_REPORT_2026-06-28.md Artifacts/Acceptance
git commit -m "test: document Happy Match polish acceptance"
```

Only after every gate is proven may the work be merged back to the user's main checkout and presented for manual acceptance.
