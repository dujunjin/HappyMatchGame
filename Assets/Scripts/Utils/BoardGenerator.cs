using UnityEngine;

/// <summary>
/// Produces the initial CellData[,] for a board from a LevelConfig.
/// Deterministic given config.seed: seeds Unity's RNG, then fills the grid
/// from the layout string map. Empty ('.') cells are filled with a random
/// normal element chosen to avoid forming a 3-in-a-row with already-placed
/// left/up neighbors (so the board never starts mid-match).
///
/// This replaces the old BoardController.InitializeBoard random fill;
/// BoardController.InitializeBoard now just instantiates GameObjects from
/// whatever CellData[,] BoardGenerator hands it.
/// </summary>
public static class BoardGenerator
{
    public static CellData[,] Generate(LevelConfig config)
    {
        if (config == null) config = LevelConfig.Default;

        // Seed once so the whole generation (layout '.' fills + any future
        // procedural fallback) is reproducible per level.
        Random.InitState(config.seed);

        int h = config.height < 1 ? 1 : config.height;
        int w = config.width < 1 ? 1 : config.width;
        ElementType[,] types = config.ToGrid();
        CellData[,] cells = new CellData[h, w];

        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                ElementType t = types[row, col];

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

        return cells;
    }

    /// <summary>
    /// True if placing `type` at (row,col) would complete a 3-in-a-row with
    /// the already-placed left two or up two neighbors. Only checks those
    /// directions because generation proceeds top-to-bottom, left-to-right.
    /// Operates on a CellData[,] so both BoardGenerator and DeadBoardDetector
    /// can share it.
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
