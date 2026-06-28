using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// One-click sprite import settings fixer for the Christmas-themed sprites.
/// Menu: HappyMatch → Fix Sprite Import Settings
/// 
/// Sets PPU=400 for Element_*.png and Special_*.png (256x256 sprites that
/// need to match the procedural 64px/100PPU = 0.64 world-unit size).
/// Sets PPU=100 for Icon_*.png and Background_*.png.
/// </summary>
public class SpriteImportFixer : EditorWindow
{
    [MenuItem("HappyMatch/Fix Sprite Import Settings")]
    public static void FixAllSprites()
    {
        string spritesDir = Path.Combine(Application.dataPath, "Sprites");
        if (!Directory.Exists(spritesDir))
        {
            Debug.LogError("Sprites directory not found: " + spritesDir);
            return;
        }

        string[] pngFiles = Directory.GetFiles(spritesDir, "*.png");
        int fixedCount = 0;

        foreach (string filePath in pngFiles)
        {
            string fileName = Path.GetFileName(filePath);
            string relativePath = "Assets/Sprites/" + fileName;

            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Could not get importer for: " + fileName);
                continue;
            }

            // Determine PPU based on file name
            int ppu;
            if (fileName.StartsWith("Element_") || fileName.StartsWith("Special_"))
            {
                ppu = 400;  // 256px / 400 = 0.64 world units (matches procedural sprites)
            }
            else
            {
                ppu = 100;  // Icons and background
            }

            // Apply settings
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = fileName.StartsWith("Background_") ? 2048 : 256;
            importer.wrapMode = TextureWrapMode.Clamp;

            // Save and reimport
            importer.SaveAndReimport();
            fixedCount++;

            Debug.Log($"Fixed: {fileName} → PPU={ppu}, MaxSize={importer.maxTextureSize}");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Sprite Import Fixer",
            $"Fixed {fixedCount} sprite(s) in Assets/Sprites/\n\n" +
            "Element_*.png and Special_*.png → 400 PPU\n" +
            "Icon_*.png and Background_*.png → 100 PPU",
            "OK");

        Debug.Log($"SpriteImportFixer: Fixed {fixedCount} sprites.");
    }
}
