using UnityEngine;

/// <summary>
/// Global constants for animation timing and color/element defaults.
/// Level-specific values (board size, step count, suitcase target, seed,
/// layout) are now authored on <see cref="LevelConfig"/>; the values kept
/// here are fallbacks used before a LevelConfig is wired up (e.g. by
/// BoardController.Awake before InitializeBoard lands the real grid) and by
/// systems that don't depend on level data.
/// </summary>
public static class GameConfig
{
    // Board dimensions (fallback only; LevelConfig.width/height is authoritative)
    public const int BoardWidth = 9;
    public const int BoardHeight = 8;
    public const float CellSize = 0.7f;
    public const float CellGap = 0.1f;

    // Element types available on the board (excluding Empty and Suitcase)
    public static readonly ElementType[] NormalElements = {
        ElementType.Red, ElementType.Blue, ElementType.Yellow, ElementType.Green
    };

    // Suitcase target (fallback only; LevelConfig.targetSuitcaseCount is authoritative)
    public const int InitialSuitcaseCount = 33;

    // Steps (fallback only; LevelConfig.maxSteps is authoritative)
    public const int MaxSteps = 25;

    // Colors for procedural sprite fallback (VisualTheme overrides these)
    public static readonly Color ElementColor_Red = new Color(0.9f, 0.2f, 0.2f);
    public static readonly Color ElementColor_Blue = new Color(0.2f, 0.4f, 0.9f);
    public static readonly Color ElementColor_Yellow = new Color(0.95f, 0.85f, 0.2f);
    public static readonly Color ElementColor_Green = new Color(0.2f, 0.8f, 0.3f);
    public static readonly Color ElementColor_Suitcase = new Color(0.9f, 0.6f, 0.1f);

    // Animation durations (seconds) — global, not level-specific
    public const float SwapDuration = 0.2f;
    public const float ClearDuration = 0.2f;
    public const float FallDuration = 0.15f;
    public const float RefillDelay = 0.05f;

    // Special types
    public enum SpecialType { None, Rocket, Bomb }

    // Rocket direction
    public enum RocketDir { Horizontal, Vertical }
}
