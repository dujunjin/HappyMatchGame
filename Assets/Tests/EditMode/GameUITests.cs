using NUnit.Framework;
using UnityEngine;

public class GameUITests
{
    [Test]
    public void Init_ReusesConfiguredTopBarChild()
    {
        GameObject root = new GameObject("GameUI");
        root.AddComponent<Canvas>();
        GameUI gameUI = root.AddComponent<GameUI>();

        GameObject topBarObject = new GameObject("TopBarView");
        topBarObject.transform.SetParent(root.transform, false);
        TopBarView configuredTopBar = topBarObject.AddComponent<TopBarView>();

        try
        {
            gameUI.Init(null);

            TopBarView[] topBars = root.GetComponentsInChildren<TopBarView>(true);
            Assert.AreEqual(1, topBars.Length,
                "GameUI.Init should not create a duplicate when a configured TopBarView child exists.");
            Assert.AreSame(configuredTopBar, topBars[0]);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
