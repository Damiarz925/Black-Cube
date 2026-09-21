using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TooltipAuthoringBuilder
{
    public const string ItemTooltipPrefabPath="Assets/Prefabs/UI/ItemTooltip.prefab";
    public const string CurrencyTooltipPrefabPath="Assets/Resources/UI/Tooltips/CurrencyTooltip.prefab";
    public const string RelicTooltipPrefabPath="Assets/Resources/UI/Tooltips/RelicTooltip.prefab";
    [MenuItem("Black-Cube/UI Authoring/Build Tooltips")]
    public static void Build()
    {
        EnsureLockAsset();
        GameObject host=new("Tooltip Authoring Host",typeof(RectTransform));
        try
        {
            ItemTooltipUI tooltip=ItemTooltipUI.Create(host.transform);UIAuthoringScreen screen=tooltip.gameObject.AddComponent<UIAuthoringScreen>();screen.Configure(UIAuthoringScreenKind.Tooltip,"tooltip.item",AssetDatabase.LoadAssetAtPath<UIVisualLibrarySO>(UIVisualLibraryBuilder.LibraryPath));
            ItemTooltipUI prefab=PrefabUtility.SaveAsPrefabAsset(tooltip.gameObject,ItemTooltipPrefabPath).GetComponent<ItemTooltipUI>();
            GameObject root=PrefabUtility.LoadPrefabContents(PersistentUIAuthoringInstaller.GameplayPrefabPath);
            try{InventoryView view=root.GetComponentInChildren<InventoryView>(true);if(view==null)throw new InvalidOperationException("Production InventoryView is missing.");view.itemTooltipPrefab=prefab;EditorUtility.SetDirty(view);PrefabUtility.SaveAsPrefabAsset(root,PersistentUIAuthoringInstaller.GameplayPrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
            GameObject inventory=PrefabUtility.LoadPrefabContents(InventoryAuthoringBuilder.PrefabPath);
            try{InventoryView view=inventory.GetComponent<InventoryView>();if(view!=null){view.itemTooltipPrefab=prefab;EditorUtility.SetDirty(view);}PrefabUtility.SaveAsPrefabAsset(inventory,InventoryAuthoringBuilder.PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(inventory);}
            Directory.CreateDirectory("Assets/Resources/UI/Tooltips");CurrencyTooltipUI currency=CurrencyTooltipUI.BuildAuthoring(host.transform);PrefabUtility.SaveAsPrefabAsset(currency.gameObject,CurrencyTooltipPrefabPath);RelicTooltipUI relic=RelicTooltipUI.BuildAuthoring(host.transform);PrefabUtility.SaveAsPrefabAsset(relic.gameObject,RelicTooltipPrefabPath);
        }
        finally{UnityEngine.Object.DestroyImmediate(host);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("TOOLTIP AUTHORING: Built ItemTooltip prefab and installed Inventory bindings.");
    }
    static void EnsureLockAsset()
    {
        const string path="Assets/Resources/UI/TooltipLock.png";Directory.CreateDirectory(Path.GetDirectoryName(path));const int size=24;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color32[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){bool body=x>=3&&x<=20&&y>=3&&y<=13;bool shackle=y>=12&&y<=20&&x>=6&&x<=17&&(x<=8||x>=15||y>=18);if(!body&&!shackle)continue;bool rim=x<=4||x>=19||y<=4||y>=19;bool keyhole=body&&x>=11&&x<=12&&y>=7&&y<=10;pixels[y*size+x]=keyhole?new Color32(28,45,48,255):rim?new Color32(45,74,75,255):new Color32(159,200,188,255);}texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.SaveAndReimport();
    }
}
