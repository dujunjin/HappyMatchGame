using UnityEngine;

public static class GameConfig
{
    // Board dimensions
    public const int BoardWidth = 9;
    public const int BoardHeight = 8;
    public const float CellSize = 0.7f;
    public const float CellGap = 0.1f;

    // Element types available on the board (excluding Empty and Suitcase)
    public static readonly ElementType[] NormalElements = {
        ElementType.Red, ElementType.Blue, ElementType.Yellow, ElementType.Green
    };

    // Suitcase settings
    public const int InitialSuitcaseCount = 33;

    // Steps
    public const int MaxSteps = 25;

    // Colors for elements
    public static readonly Color ElementColor_Red = new Color(0.9f, 0.2f, 0.2f);
    public static readonly Color ElementColor_Blue = new Color(0.2f, 0.4f, 0.9f);
    public static readonly Color ElementColor_Yellow = new Color(0.95f, 0.85f, 0.2f);
    public static readonly Color ElementColor_Green = new Color(0.2f, 0.8f, 0.3f);
    public static readonly Color ElementColor_Suitcase = new Color(0.9f, 0.6f, 0.1f);

    // Animation durations (seconds)
    public const float SwapDuration = 0.2f;
    public const float ClearDuration = 0.2f;
    public const float FallDuration = 0.15f;
    public const float RefillDelay = 0.05f;

    // Special types
    public enum SpecialType { None, Rocket, Bomb }

    // Rocket direction
    public enum RocketDir { Horizontal, Vertical }
}
