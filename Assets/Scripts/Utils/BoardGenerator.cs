using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Produces the initial CellData[,] for a board from a LevelConfig.
/// Deterministic given config.seed: seeds Unity's RNG, then fills the grid
/// from the layout string map. Empty ('.') cells are filled with a random
/// normal element chosen to avoid forming a 3-in-a-row with already-placed
/// left/up neighbors (so the board never starts mid-match).
///
/// If the layout contains NO explicit suitcases ('S'), BoardGenerator places
/// config.targetSuitcaseCount suitcases randomly on top of the normal base
/// (seeded), allowing clustered distributions while avoiding any 3-suitcase
/// run. The four legal-swap cells (0,0),(0,1),(0,2),(1,2) are protected so
/// the Default layout's guaranteed swap survives.
/// </summary>
public static class BoardGenerator
{
    public static CellData[,] Generate(LevelConfig config)
    {
        if (config == null) config = LevelConfig.Default;

        // Seed once so the whole generation is reproducible per level.
        Random.InitState(config.seed);

        int h = config.height < 1 ? 1 : config.height;
        int w = config.width < 1 ? 1 : config.width;
        ElementType[,] types = config.ToGrid();
        CellData[,] cells = new CellData[h, w];

        bool layoutHasSuitcases = false;

        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                ElementType t = types[row, col];

                if (t == ElementType.Suitcase) layoutHasSuitcases = true;

                // Empty cells ('.' or unmapped) get a no-initial-match random
                // normal element. Suitcase/explicit colors are honored as-is.
                if (t == ElementType.Empty)
                {
                    int guard = 0;
                    do
                    {
                        t = GameConfig.NormalElements[Random.Range(0, GameConfig.NormalElements.Length)];
                        guard++;
                    }
                    while (WouldCauseMatch(cells, row, col, t) && guard < 64);
                }

                cells[row, col] = new CellData
                {
                    row = row,
                    col = col,
                    elementType = t,
                    gameObject = null,
                    specialType = GameConfig.SpecialType.None,
                    rocketDir = GameConfig.RocketDir.Horizontal
                };
            }
        }

        // No explicit suitcases in the layout -> place them randomly so the
        // distribution looks natural (and may cluster), while keeping the
        // Default layout's guaranteed legal swap intact.
        if (!layoutHasSuitcases && config.targetSuitcaseCount > 0)
        {
            PlaceSuitcasesRandomly(cells, config.targetSuitcaseCount);
        }

        return cells;
    }

    /// <summary>
    /// Place `count` suitcases on random normal-element cells, skipping any
    /// that would create a 3-in-a-row of suitcases (checked in all four
    /// directions, so order doesn't matter) and the four protected legal-swap
    /// cells. A fallback pass places any remainder without the run-check so
    /// the count always matches the target (rare; the run-check is loose
    /// enough that 33/68 is trivially placeable).
    /// </summary>
    private static void PlaceSuitcasesRandomly(CellData[,] cells, int count)
    {
        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        // Protected cells keep the Default layout's legal swap available.
        var protectedCells = new HashSet<(int, int)>();
        TryAdd(protectedCells, 0, 0, rows, cols);
        TryAdd(protectedCells, 0, 1, rows, cols);
        TryAdd(protectedCells, 0, 2, rows, cols);
        TryAdd(protectedCells, 1, 2, rows, cols);

        var candidates = new List<(int, int)>();
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                ElementType t = cells[row, col].elementType;
                if (t == ElementType.Red || t == ElementType.Blue || t == ElementType.Yellow || t == ElementType.Green)
                    if (!protectedCells.Contains((row, col)))
                        candidates.Add((row, col));
            }

        Shuffle(candidates);

        int placed = 0;
        foreach (var (row, col) in candidates)
        {
            if (placed >= count) break;
            if (WouldFormSuitcaseRun(cells, row, col)) continue;
            cells[row, col].elementType = ElementType.Suitcase;
            placed++;
        }

        // Fallback for any remainder (rare).
        if (placed < count)
        {
            foreach (var (row, col) in candidates)
            {
                if (placed >= count) break;
                if (cells[row, col].elementType != ElementType.Suitcase)
                {
                    cells[row, col].elementType = ElementType.Suitcase;
                    placed++;
                }
            }
        }
    }

    /// <summary>
    /// True if placing a Suitcase at (row,col) would complete a 3-in-a-row of
    /// suitcases horizontally or vertically. Counts consecutive suitcase
    /// neighbors in both directions (so it works regardless of placement
    /// order, unlike WouldCauseMatch which only looks left/up).
    /// </summary>
    private static bool WouldFormSuitcaseRun(CellData[,] cells, int row, int col)
    {
        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        int horizontal = 1; // the cell we'd place
        for (int c = col - 1; c >= 0 && cells[row, c].elementType == ElementType.Suitcase; c--) horizontal++;
        for (int c = col + 1; c < cols && cells[row, c].elementType == ElementType.Suitcase; c++) horizontal++;
        if (horizontal >= 3) return true;

        int vertical = 1;
        for (int r = row - 1; r >= 0 && cells[r, col].elementType == ElementType.Suitcase; r--) vertical++;
        for (int r = row + 1; r < rows && cells[r, col].elementType == ElementType.Suitcase; r++) vertical++;
        return vertical >= 3;
    }

    private static void TryAdd(HashSet<(int, int)> set, int r, int c, int rows, int cols)
    {
        if (r >= 0 && r < rows && c >= 0 && c < cols)
            set.Add((r, c));
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>
    /// True if placing `type` at (row,col) would complete a 3-in-a-row with
    /// the already-placed left two or up two neighbors. Only checks those
    /// directions because normal-element generation proceeds top-to-bottom,
    /// left-to-right. Operates on a CellData[,] so both BoardGenerator and
    /// DeadBoardDetector can share it.
    /// </summary>
    public static bool WouldCauseMatch(CellData[,] cells, int row, int col, ElementType type)
    {
        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        // Horizontal: left two
        if (col >= 2 &&
            cells[row, col - 1].elementType == type &&
            cells[row, col - 2].elementType == type)
            return true;

        // Vertical: up two
        if (row >= 2 &&
            cells[row - 1, col].elementType == type &&
            cells[row - 2, col].elementType == type)
            return true;

        return false;
    }
}
