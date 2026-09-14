// Developer map: Asset-writing builder for TMP damage-number materials and prefab wiring; also exposes a Play preview.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DamageNumberStyleBuilder
{
    const string Folder="Assets/Prefabs/PaperBattle/";
    [MenuItem("Black Cube/Apply Damage Number Styles")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)return;
        var root=PrefabUtility.LoadPrefabContents(Folder+"PaperBattle.prefab");
        try {Configure(root);PrefabUtility.SaveAsPrefabAsset(root,Folder+"PaperBattle.prefab");}
        finally {PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Debug.Log("PAPER2D: Seven outlined damage-number material assets applied.");
    }
    public static void Configure(GameObject root)
    {
        var popup=root.GetComponentInChildren<DamagePopup>(true);var so=new SerializedObject(popup);
        var original=(GameObject)so.FindProperty("popupPrefab").objectReferenceValue;
        var source=original.GetComponentInChildren<TMP_Text>(true).fontSharedMaterial;
        var names=new[]{"Physical","Fire","Cold","Lightning","Poison","Bleed","Void"};
        var colors=new[]{new Color(.46f,.5f,.57f),new Color(1,.24f,.035f),new Color(.05f,.63f,1),new Color(1,.8f,.035f),new Color(.2f,.88f,.16f),new Color(.94f,.05f,.18f),new Color(.65f,.35f,1)};
        var array=so.FindProperty("numberMaterials");array.arraySize=names.Length;
        for(int i=0;i<names.Length;i++)
        {
            string path=Folder+"Damage"+names[i]+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}else mat.CopyPropertiesFromMaterial(source);
            mat.SetColor("_FaceColor",Color.Lerp(Color.white,colors[i],.18f));
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_FaceDilate",.08f);mat.SetColor("_OutlineColor",colors[i]);mat.SetFloat("_OutlineWidth",.3f);
            mat.EnableKeyword("UNDERLAY_ON");mat.SetColor("_UnderlayColor",new Color(.012f,.014f,.02f,1));
            mat.SetFloat("_UnderlayOffsetX",0);mat.SetFloat("_UnderlayOffsetY",-.2f);mat.SetFloat("_UnderlayDilate",.6f);mat.SetFloat("_UnderlaySoftness",.05f);
            EditorUtility.SetDirty(mat);array.GetArrayElementAtIndex(i).objectReferenceValue=mat;
        }
        var atlasNames=new[]{"Physical","Fire","Cold","Lightning","Poison","Bleed","Ignite"};
        var atlases=so.FindProperty("numberAtlases");atlases.arraySize=atlasNames.Length;
        for(int i=0;i<atlasNames.Length;i++)
        {
            string path="Assets/Art/PaperBattle/DamageNumbers/"+atlasNames[i]+"Digits.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null)
            {
                bool changed=importer.textureType!=TextureImporterType.Default || importer.alphaSource!=TextureImporterAlphaSource.FromInput || importer.mipmapEnabled;
                importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                if(changed)importer.SaveAndReimport();
            }
            atlases.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        so.FindProperty("digitHeight").floatValue=52f;so.FindProperty("digitSpacing").floatValue=-2f;so.FindProperty("maxNumberWidth").floatValue=180f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("Black Cube/Play Checks/Toggle Damage Style Preview")]
    static void Preview()
    {
        if(!EditorApplication.isPlaying)return;
        var previous=GameObject.Find("Damage Style Preview");if(previous){Object.Destroy(previous);return;}
        var hud=Object.FindFirstObjectByType<PaperBattleHUD>();var canvas=hud.GetComponentInParent<Canvas>();
        var panel=new GameObject("Damage Style Preview",typeof(RectTransform));panel.transform.SetParent(canvas.transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,.64f);rect.anchorMax=new Vector2(.9f,.8f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var names=new[]{"Physical","Fire","Cold","Lightning","Poison","Bleed","Ignite"};
        var popup=(GameObject)new SerializedObject(Object.FindFirstObjectByType<DamagePopup>()).FindProperty("popupPrefab").objectReferenceValue;
        var font=popup.GetComponentInChildren<TMP_Text>(true).font;
        for(int i=0;i<names.Length;i++)
        {
            var go=new GameObject(names[i],typeof(RectTransform),typeof(DamageDigitDisplay));go.transform.SetParent(panel.transform,false);
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2((i+.5f)/names.Length,.62f);r.anchoredPosition=Vector2.zero;
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/PaperBattle/DamageNumbers/"+names[i]+"Digits.png");
            go.GetComponent<DamageDigitDisplay>().SetValue(atlas,"128",52f,-2f);
            var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=new Vector2(i/(float)names.Length,0);lr.anchorMax=new Vector2((i+1)/(float)names.Length,.2f);lr.offsetMin=lr.offsetMax=Vector2.zero;
            var lt=label.GetComponent<TextMeshProUGUI>();lt.font=font;lt.text=names[i].ToUpperInvariant();lt.fontSize=14;lt.color=Color.black;lt.alignment=TextAlignmentOptions.Center;lt.raycastTarget=false;
        }
    }
}
