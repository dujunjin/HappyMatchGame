using TMPro;
using UnityEngine;

/// <summary>
/// Stable runtime boundary for the curated assets mirrored from the user's
/// top-level 图片 and font folders. Callers retain procedural fallbacks when a
/// delivery file is missing, so a broken art reference never breaks gameplay.
/// </summary>
public static class HappyMatchAssetCatalog
{
    public const string RedPath = "HappyMatch/Pieces/Red";
    public const string YellowPath = "HappyMatch/Pieces/Yellow";
    public const string BluePath = "HappyMatch/Pieces/Blue";
    public const string GreenPath = "HappyMatch/Pieces/Green";
    public const string SuitcasePath = "HappyMatch/Pieces/Suitcase";
    public const string RocketPath = "HappyMatch/Specials/Rocket";
    public const string BombPath = "HappyMatch/Specials/Bomb";
    public const string PropellerPath = "HappyMatch/Specials/Propeller";
    public const string TargetPanelPath = "HappyMatch/UI/TargetPanel";
    public const string HudFontPath = "HappyMatch/Fonts/PassionOne";
    public const string DisplayFontPath = "HappyMatch/Fonts/PoetsenOne";

    public static readonly string[] CoreSpritePaths =
    {
        RedPath, YellowPath, BluePath, GreenPath, SuitcasePath,
        RocketPath, BombPath, PropellerPath, TargetPanelPath
    };

    private static TMP_FontAsset _hudFont;
    private static TMP_FontAsset _displayFont;

    public static Sprite GetElementSprite(ElementType type)
    {
        switch (type)
        {
            case ElementType.Red: return Resources.Load<Sprite>(RedPath);
            case ElementType.Yellow: return Resources.Load<Sprite>(YellowPath);
            case ElementType.Blue: return Resources.Load<Sprite>(BluePath);
            case ElementType.Green: return Resources.Load<Sprite>(GreenPath);
            case ElementType.Suitcase: return Resources.Load<Sprite>(SuitcasePath);
            default: return null;
        }
    }

    public static Sprite GetSpecialSprite(GameConfig.SpecialType type)
    {
        switch (type)
        {
            case GameConfig.SpecialType.Rocket: return Resources.Load<Sprite>(RocketPath);
            case GameConfig.SpecialType.Bomb: return Resources.Load<Sprite>(BombPath);
            case GameConfig.SpecialType.Propeller: return Resources.Load<Sprite>(PropellerPath);
            default: return null;
        }
    }

    public static Sprite TargetPanel => Resources.Load<Sprite>(TargetPanelPath);
    public static Sprite Suitcase => Resources.Load<Sprite>(SuitcasePath);

    public static bool HasProvidedCoreAssets
    {
        get
        {
            foreach (string path in CoreSpritePaths)
                if (Resources.Load<Sprite>(path) == null) return false;
            return Resources.Load<Font>(HudFontPath) != null &&
                   Resources.Load<Font>(DisplayFontPath) != null;
        }
    }

    public static bool ApplyHudFont(TMP_Text text) => ApplyFont(text, ref _hudFont, HudFontPath);
    public static bool ApplyDisplayFont(TMP_Text text) => ApplyFont(text, ref _displayFont, DisplayFontPath);

    private static bool ApplyFont(TMP_Text text, ref TMP_FontAsset cached, string resourcePath)
    {
        if (text == null) return false;
        if (cached == null)
        {
            Font source = Resources.Load<Font>(resourcePath);
            if (source == null) return false;
            cached = TMP_FontAsset.CreateFontAsset(source);
            cached.name = source.name + " Runtime SDF";
        }

        text.font = cached;
        return true;
    }
}
