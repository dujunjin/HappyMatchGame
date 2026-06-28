using NUnit.Framework;

public class BootstrapTests
{
    [TestCase("", true, true)]
    [TestCase(null, true, true)]
    [TestCase("Assets/Scenes/SampleScene.unity", true, false)]
    [TestCase("", false, false)]
    public void ShouldLoadPlayableScene_OnlyRepairsAnEmptyLoadableScene(
        string activeScenePath, bool canLoadConfiguredScene, bool expected)
    {
        Assert.AreEqual(
            expected,
            Bootstrap.ShouldLoadPlayableScene(activeScenePath, canLoadConfiguredScene));
    }
}
