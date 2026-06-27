using NUnit.Framework;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Phase F: EditMode tests for MatchDetector — the merge fix (overlapping
/// matches union into one group, so a T-shape's vertical arm is no longer
/// dropped) and the special-pattern detection (4-line -> rocket, 5-line ->
/// propeller, T/L cross -> bomb). Uses a reflection-set BoardController so
/// no GameManager/sprites are needed.
/// </summary>
public class MatchDetectorTests
{
    private GameObject _go;

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        _go = null;
    }

    [Test]
    public void HorizontalThree_ProducesOneMatchOfThree()
    {
        var bc = MakeBoard(new ElementType[,]
        {
            { ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Blue, ElementType.Blue },
        });
        var md = new MatchDetector();
        md.Init(bc);
        var matches = md.FindAllMatches();
        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual(3, matches[0].Count);
    }

    [Test]
    public void Merge_TShape_UnionsIntoOneGroupOfFive()
    {
        // T: horizontal row 2 cols 0-2 + vertical col 1 rows 1-3, crossing at (2,1).
        var types = new ElementType[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++) types[r, c] = ElementType.Empty;
        types[2, 0] = ElementType.Red; types[2, 1] = ElementType.Red; types[2, 2] = ElementType.Red;
        types[1, 1] = ElementType.Red; types[3, 1] = ElementType.Red;

        var bc = MakeBoard(types);
        var md = new MatchDetector();
        md.Init(bc);
        var matches = md.FindAllMatches();
        Assert.AreEqual(1, matches.Count, "T-shape must merge into a single group");
        Assert.AreEqual(5, matches[0].Count, "T-shape has 5 cells");
    }

    [Test]
    public void DetectSpecial_FourLine_YieldsRocket()
    {
        var bc = MakeBoard(new ElementType[,]
        {
            { ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Blue },
        });
        var md = new MatchDetector();
        md.Init(bc);
        var matches = md.FindAllMatches();
        var (rockets, bombs, propellers) = md.DetectSpecialPatterns(matches);
        Assert.AreEqual(1, rockets.Count);
        Assert.AreEqual(0, bombs.Count);
        Assert.AreEqual(0, propellers.Count);
    }

    [Test]
    public void DetectSpecial_FiveLine_YieldsPropeller()
    {
        var bc = MakeBoard(new ElementType[,]
        {
            { ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Red, ElementType.Blue },
        });
        var md = new MatchDetector();
        md.Init(bc);
        var matches = md.FindAllMatches();
        var (rockets, bombs, propellers) = md.DetectSpecialPatterns(matches);
        Assert.AreEqual(0, rockets.Count);
        Assert.AreEqual(0, bombs.Count);
        Assert.AreEqual(1, propellers.Count);
    }

    [Test]
    public void DetectSpecial_TJunction_YieldsBomb()
    {
        var types = new ElementType[5, 5];
        for (int r = 0; r < 5; r++)
            for (int c = 0; c < 5; c++) types[r, c] = ElementType.Empty;
        types[2, 0] = ElementType.Red; types[2, 1] = ElementType.Red; types[2, 2] = ElementType.Red;
        types[1, 1] = ElementType.Red; types[3, 1] = ElementType.Red;

        var bc = MakeBoard(types);
        var md = new MatchDetector();
        md.Init(bc);
        var matches = md.FindAllMatches();
        var (rockets, bombs, propellers) = md.DetectSpecialPatterns(matches);
        Assert.AreEqual(1, bombs.Count, "T-junction must yield a bomb");
        Assert.AreEqual(0, rockets.Count, "A T yields a bomb, not a rocket");
        Assert.AreEqual(0, propellers.Count);
    }

    // --- helper: build a BoardController with cells set via reflection ---

    private BoardController MakeBoard(ElementType[,] types)
    {
        _go = new GameObject("TestBoard");
        BoardController bc = _go.AddComponent<BoardController>();
        var t = typeof(BoardController);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        int rows = types.GetLength(0), cols = types.GetLength(1);
        CellData[,] cells = new CellData[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                cells[r, c] = new CellData
                {
                    row = r,
                    col = c,
                    elementType = types[r, c],
                    gameObject = null,
                    specialType = GameConfig.SpecialType.None,
                    rocketDir = GameConfig.RocketDir.Horizontal
                };
        t.GetField("_rows", flags).SetValue(bc, rows);
        t.GetField("_cols", flags).SetValue(bc, cols);
        t.GetField("_cells", flags).SetValue(bc, cells);
        return bc;
    }
}
