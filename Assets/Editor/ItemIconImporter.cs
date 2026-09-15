// Keeps the extracted inventory tiles crisp and loadable as Texture2D, and
// imports only the six supplied Ancient currency PNGs as transparent sprites.
using UnityEditor;
using System;

public sealed class ItemIconImporter : AssetPostprocessor
{
    public static void EnsureAncientSpriteImports()
    {
        foreach (CraftingCurrencyType type in Enum.GetValues(typeof(CraftingCurrencyType)))
        {
            if (!CurrencyInventory.IsAncient(type)) continue;
            string path="Assets/Resources/"+InventoryArtCatalog.ResourcePath(type)+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing Ancient currency PNG: "+path);
            if(importer.textureType==TextureImporterType.Sprite && !importer.mipmapEnabled
                && importer.alphaIsTransparency && importer.textureCompression==TextureImporterCompression.Uncompressed)
                continue;
            importer.textureType=TextureImporterType.Sprite;
            importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.SaveAndReimport();
        }
    }

    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith("Assets/Resources/UI/Currency/Ancient"))
        {
            var currencyImporter = (TextureImporter)assetImporter;
            currencyImporter.textureType = TextureImporterType.Sprite;
            currencyImporter.alphaIsTransparency = true;
            currencyImporter.mipmapEnabled = false;
            currencyImporter.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            currencyImporter.filterMode = UnityEngine.FilterMode.Bilinear;
            currencyImporter.textureCompression = TextureImporterCompression.Uncompressed;
            currencyImporter.maxTextureSize = 2048;
            return;
        }
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
