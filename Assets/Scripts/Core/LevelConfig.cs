using UnityEngine;

/// <summary>
/// ScriptableObject describing a single Happy Match level.
/// Owns the fixed initial board layout (as a string[] row map), the random
/// seed for deterministic generation, and the win/lose targets (steps +
/// suitcases). The authoritative source for level values; GameConfig only
/// keeps global animation/UI constants.
///
/// Layout character map (one char per cell, layout[0] is the TOP row):
///   'R' -> Red      'B' -> Blue     'Y' -> Yellow   'G' -> Green
///   'S' -> Suitcase '.' or ' ' -> Empty (filled by BoardGenerator with a
///                                  no-initial-match random element)
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "HappyMatch/LevelConfig", order = 0)]
public class LevelConfig : ScriptableObject
{
    [Header("Determinism")]
    public int seed = 12345;

    [Header("Goals")]
    public int maxSteps = 25;
    public int targetSuitcaseCount = 33;

    [Header("Board Size")]
    public int width = 9;
    public int height = 8;

    [Header("Layout (row 0 = top). Empty/null => random fill.")]
    [TextArea(8, 12)]
    public string[] layout;

    /// <summary>
    /// Parse the layout string array into an ElementType[row, col] grid.
    /// row 0 corresponds to layout[0] (top of the board). Cells whose char is
    /// not recognized default to Empty (to be filled by BoardGenerator).
    /// </summary>
    public ElementType[,] ToGrid()
    {
        int h = height < 1 ? 1 : height;
        int w = width < 1 ? 1 : width;
        ElementType[,] grid = new ElementType[h, w];

        bool hasLayout = layout != null && layout.Length > 0;

        for (int row = 0; row < h; row++)
        {
            string line = (hasLayout && row < layout.Length) ? (layout[row] ?? string.Empty) : string.Empty;
            for (int col = 0; col < w; col++)
            {
                grid[row, col] = CharToElementType(col < line.Length ? line[col] : '.');
            }
        }

        return grid;
    }

    /// <summary>
    /// Count of suitcase ('S') cells in the layout. Falls back to
    /// targetSuitcaseCount when no layout is provided.
    /// </summary>
    public int CountSuitcasesInLayout()
    {
        if (layout == null || layout.Length == 0) return targetSuitcaseCount;
        int count = 0;
        foreach (string line in layout)
        {
            if (string.IsNullOrEmpty(line)) continue;
            foreach (char ch in line)
                if (ch == 'S' || ch == 's') count++;
        }
        return count;
    }

    public static ElementType CharToElementType(char ch)
    {
        switch (ch)
        {
            case 'R': case 'r': return ElementType.Red;
            case 'B': case 'b': return ElementType.Blue;
            case 'Y': case 'y': return ElementType.Yellow;
            case 'G': case 'g': return ElementType.Green;
            case 'S': case 's': return ElementType.Suitcase;
            default: return ElementType.Empty;
        }
    }

    // ---------------------------------------------------------------------
    //  Default fixed layout (normal-element base only)
    // ---------------------------------------------------------------------
    //
    // 9x8 board. The (c + row) mod 4 periodic color pattern has no 3-in-a-row
    // among normal elements. Two cells are tweaked to create a guaranteed
    // legal swap: (0,1) B->R and (1,2) G->R, so swapping (0,2)=Y with
    // (1,2)=R yields row0 cols 0-2 = R R R.
    //
    // Suitcases are NOT in the layout string — BoardGenerator places
    // targetSuitcaseCount (33) of them randomly (seeded) on top of this base,
    // allowing clustered/random distributions while avoiding any 3-suitcase
    // run. The four legal-swap cells (0,0),(0,1),(0,2),(1,2) are protected
    // from suitcase placement so the guaranteed swap always survives.
    private static readonly string[] DefaultLayout = new string[]
    {
        "RRYGRBYGR", // row 0 (top)  — (0,1) B->R
        "BYRRBYGRB", // row 1        — (1,2) G->R
        "YGRBYGRBY", // row 2
        "GRBYGRBYG", // row 3
        "RBYGRBYGR", // row 4
        "BYGRBYGRB", // row 5
        "YGRBYGRBY", // row 6
        "GRBYGRBYG", // row 7 (bottom)
    };

    private static LevelConfig _default;

    /// <summary>
    /// In-memory default LevelConfig (created via CreateInstance so it is a
    /// proper ScriptableObject). Cached for the lifetime of the process.
    /// Assign a real asset in the inspector to override.
    /// </summary>
    public static LevelConfig Default
    {
        get
        {
            if (_default == null)
            {
                _default = CreateInstance<LevelConfig>();
                _default.seed = 12345;
                _default.maxSteps = 25;
                _default.targetSuitcaseCount = 33;
                _default.width = 9;
                _default.height = 8;
                _default.layout = (string[])DefaultLayout.Clone();
            }
            return _default;
        }
    }
}
