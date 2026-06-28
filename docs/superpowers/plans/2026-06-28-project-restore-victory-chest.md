# Happy Match Project Restore and Victory Chest Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore the read-only worktree's missing empty-scene protection into the main project and replace the incorrect planar chest lid with a polished, physically readable five-state victory opening.

**Architecture:** Keep gameplay systems untouched. Add two narrow startup guards, a pure `VictoryChestMotion` timing helper covered by EditMode tests, and drive the existing `WinSequence` with five imported Sprite states plus a procedural fallback.

**Tech Stack:** Unity 2022.3.62f3c1, C#, NUnit EditMode tests, SpriteRenderer, Resources, Unity batchmode.

---

### Task 1: Restore the playable-scene guards

**Files:**
- Create: `Assets/Tests/EditMode/BootstrapTests.cs`
- Modify: `Assets/Scripts/Bootstrap/Bootstrap.cs`
- Create: `Assets/Editor/HappyMatchSceneGuard.cs`

- [x] Add a failing test for `Bootstrap.ShouldLoadPlayableScene`, run EditMode tests, and confirm the API is missing.
- [x] Add the runtime decision helper and scene load entry point from the read-only worktree.
- [x] Add the Editor-only disposable-scene guard and rerun the test to green.

### Task 2: Lock the correct chest motion contract

**Files:**
- Create: `Assets/Tests/EditMode/VictoryChestMotionTests.cs`
- Create: `Assets/Scripts/Presentation/VictoryChestMotion.cs`

- [x] Write tests requiring Closed -> Cracked -> Ajar -> Wide -> Open ordering, clamped progress, normalized crossfade weights and final Open state.
- [x] Run tests and confirm `VictoryChestMotion` is missing.
- [x] Implement only the pure timing/state functions needed by the tests, then rerun all EditMode tests.

### Task 3: Import the polished chest states

**Files:**
- Create: `Assets/Resources/VictoryChest/ChestClosed.png`
- Create: `Assets/Resources/VictoryChest/ChestCracked.png`
- Create: `Assets/Resources/VictoryChest/ChestAjar.png`
- Create: `Assets/Resources/VictoryChest/ChestWide.png`
- Create: `Assets/Resources/VictoryChest/ChestOpen.png`
- Create: matching Unity `.meta` files

- [x] Validate alpha, transparent corners, subject coverage and 1254x1254 dimensions.
- [x] Import each file as a single Sprite with mipmaps disabled and alpha transparency enabled.
- [x] Record the generated-asset provenance in `Document/ASSET_LICENSES.md`.

### Task 4: Replace the incorrect lid rotation

**Files:**
- Modify: `Assets/Scripts/UI/WinSequence.cs`

- [x] Load the five Sprite states with a procedural fallback.
- [x] Replace the Z-axis lid rotation with a 0.76-second state crossfade driven by `VictoryChestMotion`.
- [x] Keep the chest body anchored while adding latch anticipation, mouth glow, shockwave, star rays and treasure arcs.
- [x] Delay action buttons until the visual burst settles and keep both inside the portrait safe area.

### Task 5: Strengthen deterministic visual acceptance

**Files:**
- Modify: `Assets/Scripts/Utils/AcceptanceDirector.cs`
- Modify: `Document/ACCEPTANCE_REPORT_2026-06-28.md`

- [x] Capture the five authored opening milestones and final-open proof.
- [x] Run the scripted standalone acceptance and regenerate screenshots/video.
- [x] Inspect the final screenshot for correct hinge direction, no detached lid, readable `Great!`, and no UI overlap.

### Task 6: Full verification

- [x] Run all Unity EditMode tests and require zero failures.
- [x] Build the macOS player and require `BuildResult.Succeeded`.
- [x] Run the acceptance player to completion and verify the marker, screenshots and video.
- [x] Compare read-only worktree SHA-256 summary with the baseline.
- [x] Review `git diff --check`, Git status and the design checklist before reporting completion.
