using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the selected user-provided sprites sharp, transparent and sized for
/// the 9x8 board. The source folders remain outside Assets; only the curated
/// mirrors below Resources/HappyMatch are imported by Unity.
/// </summary>
public sealed class HappyMatchProvidedAssetImporter : AssetPostprocessor
{
    private const string Root = "Assets/Resources/HappyMatch/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        Apply(importer, assetPath);
    }

    [MenuItem("HappyMatch/Apply Provided Asset Import Settings")]
    public static void ApplyAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            Apply(importer, path);
            importer.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
    }

    private static void Apply(TextureImporter importer, string path)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.spritePixelsPerUnit = PixelsPerUnit(path);
    }

    private static float PixelsPerUnit(string path)
    {
        if (path.Contains("/UI/")) return 100f;
        if (path.EndsWith("Suitcase.png")) return 128f;
        if (path.EndsWith("Propeller.png")) return 140f;
        if (path.Contains("/Specials/")) return 225f;
        return 160f;
    }
}
