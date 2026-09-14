// Keeps the extracted 96px inventory tiles and highlight border crisp and free
// from mip bleeding while leaving them loadable as Texture2D resources.
using UnityEditor;

public sealed class ItemIconImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/UI/ItemIcons/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        bool isBorder = assetPath.EndsWith("mod_highlight_border.png") || assetPath.Contains("corruption_border_");
        importer.maxTextureSize = isBorder ? 256 : 128;
    }
}
