using NUnit.Framework;
using UnityEngine;

public class VictoryChestMotionTests
{
    [Test]
    public void Evaluate_StartsClosedAndFinishesOpen()
    {
        VictoryChestMotion.Weights start = VictoryChestMotion.Evaluate(0f);
        VictoryChestMotion.Weights end = VictoryChestMotion.Evaluate(1f);

        Assert.AreEqual(1f, start.closed, 0.0001f);
        Assert.AreEqual(0f, start.cracked, 0.0001f);
        Assert.AreEqual(0f, start.ajar, 0.0001f);
        Assert.AreEqual(0f, start.wide, 0.0001f);
        Assert.AreEqual(0f, start.open, 0.0001f);
        Assert.AreEqual(0f, end.closed, 0.0001f);
        Assert.AreEqual(0f, end.cracked, 0.0001f);
        Assert.AreEqual(0f, end.ajar, 0.0001f);
        Assert.AreEqual(0f, end.wide, 0.0001f);
        Assert.AreEqual(1f, end.open, 0.0001f);
    }

    [Test]
    public void Evaluate_PassesThroughReadableAjarPose()
    {
        VictoryChestMotion.Weights halfway = VictoryChestMotion.Evaluate(0.46f);

        Assert.Greater(halfway.ajar, 0.95f);
        Assert.Less(halfway.closed, 0.05f);
        Assert.Less(halfway.open, 0.05f);
    }

    [Test]
    public void Evaluate_PassesThroughEarlyAndLateIntermediatePoses()
    {
        VictoryChestMotion.Weights early = VictoryChestMotion.Evaluate(0.23f);
        VictoryChestMotion.Weights late = VictoryChestMotion.Evaluate(0.72f);

        Assert.Greater(early.cracked, 0.95f);
        Assert.Less(early.closed + early.ajar + early.wide + early.open, 0.05f);
        Assert.Greater(late.wide, 0.95f);
        Assert.Less(late.closed + late.cracked + late.ajar + late.open, 0.05f);
    }

    [TestCase(-1f)]
    [TestCase(0f)]
    [TestCase(0.2f)]
    [TestCase(0.46f)]
    [TestCase(0.72f)]
    [TestCase(1f)]
    [TestCase(2f)]
    public void Evaluate_AlwaysReturnsNormalizedNonNegativeWeights(float progress)
    {
        VictoryChestMotion.Weights weights = VictoryChestMotion.Evaluate(progress);

        Assert.That(weights.closed, Is.InRange(0f, 1f));
        Assert.That(weights.cracked, Is.InRange(0f, 1f));
        Assert.That(weights.ajar, Is.InRange(0f, 1f));
        Assert.That(weights.wide, Is.InRange(0f, 1f));
        Assert.That(weights.open, Is.InRange(0f, 1f));
        Assert.AreEqual(1f, weights.closed + weights.cracked + weights.ajar + weights.wide + weights.open, 0.0001f);
    }

    [Test]
    public void NormalizedProgress_ClampsToOpeningDuration()
    {
        Assert.AreEqual(0f, VictoryChestMotion.NormalizedProgress(-1f), 0.0001f);
        Assert.AreEqual(1f, VictoryChestMotion.NormalizedProgress(VictoryChestMotion.OpeningDuration + 1f), 0.0001f);
    }

    [Test]
    public void VictoryButtons_StayOrderedInsidePortraitSafeArea()
    {
        Assert.Greater(VictoryChestMotion.RetryButtonY, VictoryChestMotion.ReplayButtonY);
        Assert.That(VictoryChestMotion.RetryButtonY - VictoryChestMotion.ReplayButtonY, Is.InRange(48f, 64f));
        Assert.GreaterOrEqual(VictoryChestMotion.ReplayButtonY, -280f);
    }

    [Test]
    public void RadialGlow_HasBrightCenterAndTransparentCorners()
    {
        Sprite sprite = SpriteGenerator.CreateRadialGlowSprite(new Color(1f, 0.75f, 0.2f), 32);
        Texture2D texture = sprite.texture;

        Assert.Greater(texture.GetPixel(16, 16).a, 0.9f);
        Assert.Less(texture.GetPixel(0, 0).a, 0.01f);
        Assert.Less(texture.GetPixel(1, 16).a, 0.08f);

        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }
}
