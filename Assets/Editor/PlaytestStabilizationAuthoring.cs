using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// A targeted migration, not a UI rebuild: preserve authored node/equipment positions.
public static class PlaytestStabilizationAuthoring
{
    [MenuItem("Black-Cube/UI Authoring/Repair Playtest Interaction Bindings")]
    public static void Repair()
    {
        RepairPrefab(PassiveTreePrefabBuilder.PrefabPath);
        RepairPrefab(InventoryAuthoringBuilder.PrefabPath);
        RepairPrefab(PersistentUIAuthoringInstaller.GameplayPrefabPath);
        AssetDatabase.SaveAssets();
        Debug.Log("PLAYTEST INTERACTION BINDINGS: repaired hub raycast and inventory scroll content.");
    }
    static void RepairPrefab(string path)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var tree in root.GetComponentsInChildren<PassiveTreeView>(true))
                if(tree.playerHub!=null&&tree.playerHub.TryGetComponent<Image>(out var image))image.raycastTarget=true;
            foreach(var inventory in root.GetComponentsInChildren<InventoryView>(true))
            {
                var content=inventory.itemGridRoot;
                if(content==null)throw new InvalidOperationException("Missing inventory content: "+path);
                var scroll=content.GetComponentInParent<ScrollRect>(true);
                if(scroll==null||scroll.viewport==null)throw new InvalidOperationException("Missing inventory viewport: "+path);
                scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
                InventoryArtLayout.Apply((RectTransform)scroll.transform,InventoryArtLayout.InventoryBounds);
                if(scroll.viewport.GetComponent<RectMask2D>()==null)scroll.viewport.gameObject.AddComponent<RectMask2D>();
                var grid=content.GetComponent<GridLayoutGroup>();
                if(grid!=null){grid.cellSize=InventoryUI.GridCellSize;grid.spacing=InventoryUI.GridSpacing;}
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
