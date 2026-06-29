using NUnit.Framework;
using UnityEngine;

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

}
