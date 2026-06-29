# Happy Match Provided Asset Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace procedural core game art and default typography with the selected files from the user-provided `图片/` and `font/` folders while preserving the verified match-3 behavior.

**Architecture:** Mirror only selected source assets into a stable Resources hierarchy, then resolve them through one testable catalog. Existing `VisualTheme` remains the fallback boundary, UI classes apply catalog sprites/fonts, and the victory sequence uses the provided suitcase instead of the unrelated wooden chest.

**Tech Stack:** Unity 2022.3.62f3c1, C# 9, Unity Test Framework 1.1.33, uGUI, TextMesh Pro, macOS Player, FFmpeg.

---

## File map

- Create `Assets/Resources/HappyMatch/Pieces/*.png`: four normal pieces and suitcase.
- Create `Assets/Resources/HappyMatch/Specials/*.png`: rocket, bomb and propeller.
- Create `Assets/Resources/HappyMatch/UI/TargetPanel.png`: reference-style target panel.
- Create `Assets/Resources/HappyMatch/Fonts/*.ttf`: Passion One and Poetsen One.
- Create `Assets/Scripts/Core/HappyMatchAssetCatalog.cs`: resource paths, Sprite loading, TMP font creation and cache.
- Create `Assets/Editor/HappyMatchProvidedAssetImporter.cs`: deterministic sprite import settings.
- Create `Assets/Tests/EditMode/ProvidedAssetTests.cs`: resource and integration acceptance tests.
- Modify `Assets/Scripts/Core/VisualTheme.cs`: provided-resource-first fallback behavior.
- Modify `Assets/Scripts/UI/TopBarView.cs`: provided target panel/icon and font.
- Modify `Assets/Scripts/UI/ResultDialog.cs`: provided display font.
- Modify `Assets/Scripts/UI/WinSequence.cs`: provided display font and suitcase victory prop.
- Modify `Document/ASSET_LICENSES.md`: record user-supplied resources and retained placeholder background.
- Create `项目说明.md`: final run, status and known-issue handoff.
- Modify `Document/ACCEPTANCE_REPORT_2026-06-29.md`: fresh evidence.

### Task 1: Lock the asset contract with failing tests

**Files:**
- Create: `Assets/Tests/EditMode/ProvidedAssetTests.cs`

- [ ] Add tests asserting catalog constants for `Pieces/Red`, `Pieces/Yellow`, `Pieces/Blue`, `Pieces/Green`, `Pieces/Suitcase`, `Specials/Rocket`, `Specials/Bomb`, `Specials/Propeller`, `UI/TargetPanel`, `Fonts/PassionOne`, and `Fonts/PoetsenOne`.
- [ ] Add tests that `Resources.Load<Sprite>` returns all nine sprites and `Resources.Load<Font>` returns both fonts.
- [ ] Add a test that a new `VisualTheme` resolves the provided red piece instead of a generated fallback.
- [ ] Run `Unity -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter ProvidedAssetTests -testResults /tmp/hm-assets-red.xml -logFile /tmp/hm-assets-red.log` and verify compilation fails because `HappyMatchAssetCatalog` does not exist.
- [ ] Commit the RED tests with `git commit -m "test: define provided asset contract"`.

### Task 2: Import the selected images and fonts

**Files:**
- Create: `Assets/Resources/HappyMatch/Pieces/Red.png`
- Create: `Assets/Resources/HappyMatch/Pieces/Yellow.png`
- Create: `Assets/Resources/HappyMatch/Pieces/Blue.png`
- Create: `Assets/Resources/HappyMatch/Pieces/Green.png`
- Create: `Assets/Resources/HappyMatch/Pieces/Suitcase.png`
- Create: `Assets/Resources/HappyMatch/Specials/Rocket.png`
- Create: `Assets/Resources/HappyMatch/Specials/Bomb.png`
- Create: `Assets/Resources/HappyMatch/Specials/Propeller.png`
- Create: `Assets/Resources/HappyMatch/UI/TargetPanel.png`
- Create: `Assets/Resources/HappyMatch/Fonts/PassionOne.ttf`
- Create: `Assets/Resources/HappyMatch/Fonts/PoetsenOne.ttf`
- Create: `Assets/Editor/HappyMatchProvidedAssetImporter.cs`

- [ ] Copy the exact source files listed in the design resource map into the paths above without resampling.
- [ ] Import PNGs as single sprites with alpha transparency, mipmaps disabled, lossless compression and per-role PPU that keeps board pieces near one cell wide.
- [ ] Run Unity once in batch mode to import assets, then rerun `ProvidedAssetTests`; expected failure is now limited to missing catalog/VisualTheme behavior.
- [ ] Commit with `git commit -m "feat: import provided match art and fonts"`.

### Task 3: Implement catalog, theme and UI integration

**Files:**
- Create: `Assets/Scripts/Core/HappyMatchAssetCatalog.cs`
- Modify: `Assets/Scripts/Core/VisualTheme.cs`
- Modify: `Assets/Scripts/UI/TopBarView.cs`
- Modify: `Assets/Scripts/UI/ResultDialog.cs`
- Modify: `Assets/Scripts/UI/WinSequence.cs`
- Modify: `Assets/Tests/EditMode/ProvidedAssetTests.cs`

- [ ] Implement Sprite/Font resource constants and cached loaders in `HappyMatchAssetCatalog`; implement `ApplyHudFont` and `ApplyDisplayFont` for TMP text.
- [ ] Make `VisualTheme` resolve Inspector override, then provided Sprite, then procedural fallback in that order.
- [ ] Make `TopBarView` default to provided target panel and suitcase icon, and apply Passion One to counts/labels.
- [ ] Apply Poetsen One to result and victory text; replace the five wooden chest frames with one provided suitcase renderer plus squash, spin, glow and burst motion.
- [ ] Extend `ProvidedAssetTests` to assert `TopBarView` creates `ProvidedTargetPanel` and that `WinSequence` exposes the provided suitcase resource path.
- [ ] Run focused tests and then all EditMode tests; expected result is 0 failed.
- [ ] Commit with `git commit -m "feat: use provided assets across gameplay and UI"`.

### Task 4: Build and visually validate the real player

**Files:**
- Modify: `Assets/Editor/HappyMatchAcceptanceBuild.cs` only if acceptance output paths need a new run directory.
- Update: `Artifacts/Acceptance/*` generated evidence.

- [ ] Run the full EditMode suite and save XML/log evidence.
- [ ] Run the compile-only batch entry and verify no `error CS`, unhandled exception, missing resource or `NullReference` in the log.
- [ ] Build the macOS Release Player with `HappyMatchAcceptanceBuild.BuildMacPlayer` and require exit 0.
- [ ] Launch the built player in scripted acceptance mode and wait for `acceptance-complete.txt` plus five screenshots.
- [ ] Inspect initial, special-action, target-flight and victory images; confirm the exact red/yellow/blue/green shapes, orange suitcase, target panel, specials and font are visible.
- [ ] If visual/runtime verification fails, invoke systematic debugging, add a failing regression test where feasible, fix, and rerun the complete gate.

### Task 5: Record and prepare final handoff

**Files:**
- Create: `项目说明.md`
- Modify: `Document/ASSET_LICENSES.md`
- Create: `Document/ACCEPTANCE_REPORT_2026-06-29.md`
- Create: `Artifacts/Acceptance/HappyMatchGame-指定资源版-Demo.mp4`

- [ ] Record the actual scripted Player run at 820×1022 with its game audio, encode H.264/AAC, and verify video duration, dimensions, frame rate and audio stream with `ffprobe`.
- [ ] Generate a contact sheet and inspect opening board, special effects, target reduction and victory suitcase frames.
- [ ] Copy the verified MP4 to `~/Desktop/HappyMatchGame-指定资源版-Demo.mp4`.
- [ ] Document Unity version, opening/build steps, controls, completed scope, exact asset mapping, retained background placeholder, and known issues in `项目说明.md`.
- [ ] Record test counts, build path, runtime marker, screenshot/video metadata and log scans in the acceptance report.
- [ ] Run a final clean verification, commit documentation/evidence, merge the branch back to the original workspace, and confirm the original workspace builds from the merged revision.
