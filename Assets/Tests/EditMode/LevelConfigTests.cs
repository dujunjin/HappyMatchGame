using NUnit.Framework;

/// <summary>
/// Phase F: EditMode tests for the LevelConfig defaults — dimensions, the
/// all-normal base layout (no explicit S; suitcases are procedurally placed),
/// and no opening 3-in-a-row in the base pattern.
/// </summary>
public class LevelConfigTests
{
    [Test]
    public void Default_HasExpectedDimensions()
    {
        LevelConfig cfg = LevelConfig.Default;
        Assert.AreEqual(9, cfg.width);
        Assert.AreEqual(8, cfg.height);
        Assert.AreEqual(25, cfg.maxSteps);
        Assert.AreEqual(33, cfg.targetSuitcaseCount);
    }

    [Test]
    public void Default_LayoutIsAllNormal_NoExplicitSuitcases()
    {
        // The Default layout is a normal-element base; suitcases are placed by
        // BoardGenerator on top, so the layout itself has zero 'S'.
        Assert.AreEqual(0, LevelConfig.Default.CountSuitcasesInLayout());
    }

    [Test]
    public void Default_ToGrid_MatchesDimensions()
    {
        ElementType[,] grid = LevelConfig.Default.ToGrid();
        Assert.AreEqual(8, grid.GetLength(0));
        Assert.AreEqual(9, grid.GetLength(1));
    }

    [Test]
    public void Default_ToGrid_HasNoThreeInARow()
    {
        ElementType[,] grid = LevelConfig.Default.ToGrid();
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c + 2 < cols; c++)
            {
                ElementType t = grid[r, c];
                if (t == ElementType.Empty) continue;
                Assert.IsFalse(grid[r, c + 1] == t && grid[r, c + 2] == t,
                    $"Horizontal 3-in-a-row at ({r},{c})");
            }
        for (int c = 0; c < cols; c++)
            for (int r = 0; r + 2 < rows; r++)
            {
                ElementType t = grid[r, c];
                if (t == ElementType.Empty) continue;
                Assert.IsFalse(grid[r + 1, c] == t && grid[r + 2, c] == t,
                    $"Vertical 3-in-a-row at ({r},{c})");
            }
    }
}
