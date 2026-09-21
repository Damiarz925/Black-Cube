using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>One-shot maintenance command that keeps passive icons as normal PNG assets instead of large inline texture subassets.</summary>
public static class PassiveIconLibraryExternalizer
{
    public const string IconFolder = "Assets/Resources/UI/PassiveIcons";

    [MenuItem("Black-Cube/UI Authoring/Externalize Passive Icon Textures")]
    public static void Run()
    {
        PassiveNodeIconLibrarySO library=AssetDatabase.LoadAssetAtPath<PassiveNodeIconLibrarySO>(PassiveTreeAuthoringMigration.IconLibraryPath);
        if(library==null)throw new InvalidOperationException("Passive icon library is missing.");
        Directory.CreateDirectory(IconFolder);
        UnityEngine.Object[] embedded=AssetDatabase.LoadAllAssetsAtPath(PassiveTreeAuthoringMigration.IconLibraryPath);
        Sprite[] sprites=embedded.OfType<Sprite>().ToArray();
        if(sprites.Length==0){Debug.Log("PASSIVE ICON EXTERNALIZER: Icons are already external assets.");return;}

        var external=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        foreach(Sprite source in sprites.GroupBy(x=>x.name,StringComparer.Ordinal).Select(x=>x.First()))
        {
            string safe=string.Concat(source.name.Select(c=>char.IsLetterOrDigit(c)||c is '-' or '_'?c:'-'));
            string path=$"{IconFolder}/{safe}.png";
            external[source.name]=WriteSprite(source,path);
        }

        SerializedObject serialized=new(library);
        Replace(serialized.FindProperty("genericFallback"),external);
        SerializedProperty mappings=serialized.FindProperty("mappings");
        for(int i=0;i<mappings.arraySize;i++)
        {
            SerializedProperty mapping=mappings.GetArrayElementAtIndex(i);
            Replace(mapping.FindPropertyRelative("normal"),external);Replace(mapping.FindPropertyRelative("hovered"),external);Replace(mapping.FindPropertyRelative("allocated"),external);Replace(mapping.FindPropertyRelative("unavailable"),external);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);

        foreach(UnityEngine.Object item in embedded)if(item!=null&&item!=library){AssetDatabase.RemoveObjectFromAsset(item);UnityEngine.Object.DestroyImmediate(item,true);}
        AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(PassiveTreeAuthoringMigration.IconLibraryPath,ImportAssetOptions.ForceUpdate);AssetDatabase.Refresh();
        Debug.Log($"PASSIVE ICON EXTERNALIZER: Wrote {external.Count} referenced PNG sprites to {IconFolder}.");
    }

    static void Replace(SerializedProperty property,IReadOnlyDictionary<string,Sprite> external)
    {
        Sprite old=property.objectReferenceValue as Sprite;if(old!=null&&external.TryGetValue(old.name,out Sprite replacement))property.objectReferenceValue=replacement;
    }

    static Texture2D ReadableCopy(Texture source)
    {
        RenderTexture temporary=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);RenderTexture previous=RenderTexture.active;
        Graphics.Blit(source,temporary);RenderTexture.active=temporary;Texture2D copy=new(source.width,source.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(temporary);return copy;
    }

    public static Sprite WriteSprite(Sprite source,string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));Texture2D copy=ReadableCopy(source.texture);File.WriteAllBytes(path,copy.EncodeToPNG());UnityEngine.Object.DestroyImmediate(copy);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=source.pixelsPerUnit;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
