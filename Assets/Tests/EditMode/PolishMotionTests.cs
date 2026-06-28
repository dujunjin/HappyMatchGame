using NUnit.Framework;

public class PolishMotionTests
{
    [Test]
    public void EaseOutBack_ClampsEndpoints()
    {
        Assert.AreEqual(0f, PolishMotion.EaseOutBack(0f), 0.0001f);
        Assert.AreEqual(1f, PolishMotion.EaseOutBack(1f), 0.0001f);
    }

    [Test]
    public void EaseInOutCubic_ClampsEndpoints()
    {
        Assert.AreEqual(0f, PolishMotion.EaseInOutCubic(-1f), 0.0001f);
        Assert.AreEqual(1f, PolishMotion.EaseInOutCubic(2f), 0.0001f);
    }

    [Test]
    public void FallDuration_GrowsWithDistanceAndStaysBounded()
    {
        Assert.Less(PolishMotion.FallDuration(1), PolishMotion.FallDuration(6));
        Assert.That(PolishMotion.FallDuration(20), Is.InRange(0.18f, 0.34f));
    }

    [TestCase(ElementType.Red, true)]
    [TestCase(ElementType.Blue, true)]
    [TestCase(ElementType.Yellow, true)]
    [TestCase(ElementType.Green, true)]
    [TestCase(ElementType.Suitcase, false)]
    [TestCase(ElementType.Empty, false)]
    public void IsColorMatchable_OnlyNormalElements(ElementType type, bool expected)
    {
        Assert.AreEqual(expected, PolishMotion.IsColorMatchable(type));
    }
}
