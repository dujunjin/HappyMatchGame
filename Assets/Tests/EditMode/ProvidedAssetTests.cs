using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProvidedAssetTests
{
    [Test]
    public void Catalog_DeclaresStableProvidedResourcePaths()
    {
        Assert.AreEqual("HappyMatch/Pieces/Red", HappyMatchAssetCatalog.RedPath);
        Assert.AreEqual("HappyMatch/Pieces/Yellow", HappyMatchAssetCatalog.YellowPath);
        Assert.AreEqual("HappyMatch/Pieces/Blue", HappyMatchAssetCatalog.BluePath);
        Assert.AreEqual("HappyMatch/Pieces/Green", HappyMatchAssetCatalog.GreenPath);
        Assert.AreEqual("HappyMatch/Pieces/Suitcase", HappyMatchAssetCatalog.SuitcasePath);
        Assert.AreEqual("HappyMatch/Specials/Rocket", HappyMatchAssetCatalog.RocketPath);
        Assert.AreEqual("HappyMatch/Specials/Bomb", HappyMatchAssetCatalog.BombPath);
        Assert.AreEqual("HappyMatch/Specials/Propeller", HappyMatchAssetCatalog.PropellerPath);
        Assert.AreEqual("HappyMatch/UI/TargetPanel", HappyMatchAssetCatalog.TargetPanelPath);
        Assert.AreEqual("HappyMatch/Fonts/PassionOne", HappyMatchAssetCatalog.HudFontPath);
        Assert.AreEqual("HappyMatch/Fonts/PoetsenOne", HappyMatchAssetCatalog.DisplayFontPath);
    }

    [Test]
    public void Resources_ContainEverySelectedSpriteAndFont()
    {
        foreach (string path in HappyMatchAssetCatalog.CoreSpritePaths)
            Assert.IsNotNull(Resources.Load<Sprite>(path), path);

        Assert.IsNotNull(Resources.Load<Font>(HappyMatchAssetCatalog.HudFontPath));
        Assert.IsNotNull(Resources.Load<Font>(HappyMatchAssetCatalog.DisplayFontPath));
    }

    [Test]
    public void VisualTheme_DefaultsToProvidedRedPiece()
    {
        VisualTheme theme = ScriptableObject.CreateInstance<VisualTheme>();
        Sprite expected = Resources.Load<Sprite>(HappyMatchAssetCatalog.RedPath);

        Assert.IsNotNull(expected);
        Assert.AreSame(expected, theme.GetElementSprite(ElementType.Red));

        Object.DestroyImmediate(theme);
    }

    [Test]
    public void Catalog_AppliesProvidedHudFontToTmpText()
    {
        GameObject go = new GameObject("HudText");
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();

        Assert.IsTrue(HappyMatchAssetCatalog.ApplyHudFont(text));
        Assert.IsNotNull(text.font);
        StringAssert.Contains("Passion", text.font.faceInfo.familyName);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TopBar_UsesProvidedTargetPanelAndSuitcaseIcon()
    {
        GameObject go = new GameObject("TopBarTest");
        TopBarView view = go.AddComponent<TopBarView>();
        view.Init(null);

        Image[] images = go.GetComponentsInChildren<Image>(true);
        Image pill = System.Array.Find(images, image => image.sprite == HappyMatchAssetCatalog.TargetPanel);
        Image icon = System.Array.Find(images, image => image.gameObject.name == "TargetIcon");

        Assert.IsNotNull(pill);
        Assert.AreSame(HappyMatchAssetCatalog.TargetPanel, pill.sprite);
        Assert.IsNotNull(icon);
        Assert.AreSame(HappyMatchAssetCatalog.Suitcase, icon.sprite);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void WinSequence_UsesProvidedSuitcaseAsVictoryProp()
    {
        Assert.AreEqual(HappyMatchAssetCatalog.SuitcasePath, WinSequence.VictoryPropResourcePath);
        Assert.IsNotNull(Resources.Load<Sprite>(WinSequence.VictoryPropResourcePath));
    }

}
