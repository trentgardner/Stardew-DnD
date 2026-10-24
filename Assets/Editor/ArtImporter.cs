using UnityEditor;
using UnityEngine;

/// <summary>
/// Any PNG dropped into Assets/Resources/Art/ is automatically set up as crisp pixel art:
/// Sprite, 16 pixels per unit, point filtering, no compression, and the right pivot
/// (feet for characters, monsters and objects; center for tiles, items and UI).
/// See docs/ART_IMPORT_GUIDE.md.
/// </summary>
public class ArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool standsOnGround = name.StartsWith("hero_") || name.StartsWith("npc_") || name.StartsWith("ally_") || name.StartsWith("mon_") || name.StartsWith("obj_");
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteAlignment = standsOnGround ? (int)SpriteAlignment.Custom : (int)SpriteAlignment.Center;
        settings.spritePivot = standsOnGround ? new Vector2(0.5f, 0.1f) : new Vector2(0.5f, 0.5f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        // UI panels: 4px 9-slice border so they stretch cleanly.
        if (name == "ui_panel" || name == "ui_panel_dark" || name == "ui_slot") settings.spriteBorder = new Vector4(4, 4, 4, 4);
        ti.SetTextureSettings(settings);
    }
}
