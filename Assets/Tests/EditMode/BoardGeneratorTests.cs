using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Phase F: EditMode tests for the deterministic board generator. Verifies
/// that the same seed always produces the same layout, that the Default level
/// places exactly 33 suitcases with no opening 3-in-a-row, and that the
/// guaranteed legal swap (0,2)<->(1,2) actually produces a match.
/// </summary>
public class BoardGeneratorTests
{
    [Test]
    public void Generate_SameSeed_ProducesIdenticalGrid()
    {
        CellData[,] a = BoardGenerator.Generate(LevelConfig.Default);
        CellData[,] b = BoardGenerator.Generate(LevelConfig.Default);
        Assert.AreEqual(a.GetLength(0), b.GetLength(0));
        Assert.AreEqual(a.GetLength(1), b.GetLength(1));
        for (int r = 0; r < a.GetLength(0); r++)
            for (int c = 0; c < a.GetLength(1); c++)
                Assert.AreEqual(a[r, c].elementType, b[r, c].elementType,
                    $"Differ at ({r},{c})");
    }

    [Test]
    public void Generate_Default_PlacesExactly33Suitcases()
    {
        CellData[,] grid = BoardGenerator.Generate(LevelConfig.Default);
        int count = 0;
        for (int r = 0; r < grid.GetLength(0); r++)
            for (int c = 0; c < grid.GetLength(1); c++)
                if (grid[r, c].elementType == ElementType.Suitcase) count++;
        Assert.AreEqual(33, count);
    }

    [Test]
    public void Generate_Default_HasNoOpeningThreeInARow()
    {
        CellData[,] grid = BoardGenerator.Generate(LevelConfig.Default);
        Assert.IsFalse(HasThreeInARow(grid), "Default board must not start with a 3-in-a-row");
    }

    [Test]
    public void Generate_Default_LegalSwapProducesMatch()
    {
        // The Default layout guarantees swap (0,2)<->(1,2) makes row0 cols 0-2 = R,R,R.
        CellData[,] grid = BoardGenerator.Generate(LevelConfig.Default);
        // Protected cells must still be normal elements (not suitcases).
        Assert.AreNotEqual(ElementType.Suitcase, grid[0, 0].elementType);
        Assert.AreNotEqual(ElementType.Suitcase, grid[0, 1].elementType);
        Assert.AreNotEqual(ElementType.Suitcase, grid[0, 2].elementType);
        Assert.AreNotEqual(ElementType.Suitcase, grid[1, 2].elementType);

        // Swap (0,2) and (1,2).
        ElementType t = grid[0, 2].elementType;
        grid[0, 2].elementType = grid[1, 2].elementType;
        grid[1, 2].elementType = t;

        // Row 0 cols 0-2 must now be three of the same type.
        Assert.AreEqual(grid[0, 0].elementType, grid[0, 1].elementType);
        Assert.AreEqual(grid[0, 1].elementType, grid[0, 2].elementType);
        Assert.AreNotEqual(ElementType.Empty, grid[0, 0].elementType);
    }

    [Test]
    public void WouldCauseMatch_DetectsLeftTwo()
    {
        // A 3x3 grid with two Reds on the left of row 0; placing Red at (0,2) must match.
        CellData[,] cells = new CellData[3, 3];
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                cells[r, c] = new CellData { row = r, col = c, elementType = ElementType.Empty };
        cells[0, 0].elementType = ElementType.Red;
        cells[0, 1].elementType = ElementType.Red;
        Assert.IsTrue(BoardGenerator.WouldCauseMatch(cells, 0, 2, ElementType.Red));
        Assert.IsFalse(BoardGenerator.WouldCauseMatch(cells, 0, 2, ElementType.Blue));
    }

    private static bool HasThreeInARow(CellData[,] grid)
    {
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c + 2 < cols; c++)
            {
                ElementType t = grid[r, c].elementType;
                if (t == ElementType.Empty) continue;
                if (grid[r, c + 1].elementType == t && grid[r, c + 2].elementType == t) return true;
            }
        for (int c = 0; c < cols; c++)
            for (int r = 0; r + 2 < rows; r++)
            {
                ElementType t = grid[r, c].elementType;
                if (t == ElementType.Empty) continue;
                if (grid[r + 1, c].elementType == t && grid[r + 2, c].elementType == t) return true;
            }
        return false;
    }
}
