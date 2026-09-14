// Developer map: Disposable batch-mode Play capture for the supplied-art top HUD and live Goblin binding.
using System;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TopHUDVisualVerification
{
    const string PendingKey="BlackCube.TopHUDVisualVerification.Pending";
    const string CapturePath="ReviewCaptures/top-hud-final.png";
    const string ReportPath="ReviewCaptures/top-hud-runtime.txt";
    static double enteredAt;
    static bool captured;
    static bool prepared;

    static TopHUDVisualVerification(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}

    public static void Run()
    {
        Directory.CreateDirectory("ReviewCaptures");
        SessionState.SetBool(PendingKey,true);captured=false;prepared=false;enteredAt=0;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Screen.SetResolution(1920,1080,false);
        EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(PendingKey,false))return;
        if(!EditorApplication.isPlaying)
        {
            if(captured){SessionState.EraseBool(PendingKey);EditorApplication.Exit(0);}
            return;
        }
        if(captured)return;
        var hud=UnityEngine.Object.FindAnyObjectByType<PaperBattleHUD>();
        var manager=BattleManager.Instance;
        if(hud==null||manager==null||manager.CurrentEnemyAI==null)return;
        if(!prepared)
        {
            Time.timeScale=0f;
            float target=Mathf.Min(897f,hud.player.MaxLife);float delta=hud.player.CurrentLife-target;
            if(delta>0)hud.player.LoseLife(delta);else if(delta<0)hud.player.RestoreLife(-delta);
            prepared=true;enteredAt=EditorApplication.timeSinceStartup;return;
        }
        if(EditorApplication.timeSinceStartup-enteredAt<.75)return;
        CaptureRenderedFrame(hud,manager);
        captured=true;
        EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;
    }

    static void CaptureRenderedFrame(PaperBattleHUD hud,BattleManager manager)
    {
        Canvas canvas=hud.GetComponentInParent<Canvas>();Camera camera=Camera.main??UnityEngine.Object.FindAnyObjectByType<Camera>();
        if(canvas==null||camera==null)throw new InvalidOperationException("HUD verification requires an active Canvas and Camera.");
        RenderMode oldMode=canvas.renderMode;Camera oldCanvasCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;RenderTexture oldTarget=camera.targetTexture;RenderTexture oldActive=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var screenshot=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
            Canvas.ForceUpdateCanvases();File.WriteAllText(ReportPath,Describe(hud,manager));camera.Render();RenderTexture.active=target;
            screenshot.ReadPixels(new Rect(0,0,1920,1080),0,0,false);screenshot.Apply(false,false);File.WriteAllBytes(CapturePath,screenshot.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCanvasCamera;canvas.planeDistance=oldPlane;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(screenshot);
        }
    }

    static string Describe(PaperBattleHUD hud,BattleManager manager)
    {
        var b=new StringBuilder();b.AppendLine($"screen={Screen.width}x{Screen.height}");b.AppendLine($"enemy={manager.CurrentEnemyAI.name}");
        AddText(b,"playerName",hud.playerText);AddText(b,"enemyName",hud.enemyText);
        AddText(b,"playerHP",Field<TMP_Text>(hud,"playerHealthText"));AddText(b,"playerMana",Field<TMP_Text>(hud,"playerManaText"));
        AddText(b,"enemyHP",Field<TMP_Text>(hud,"enemyHealthText"));AddText(b,"enemyMana",Field<TMP_Text>(hud,"enemyManaText"));
        AddImage(b,"playerPortrait",Field<Image>(hud,"playerPortrait"));AddImage(b,"enemyPortrait",Field<Image>(hud,"enemyPortrait"));
        AddFill(b,"playerHPFill",Field<HUDResourceBar>(hud,"playerHealthFill"));AddFill(b,"playerManaFill",Field<HUDResourceBar>(hud,"playerManaFill"));
        AddFill(b,"enemyHPFill",Field<HUDResourceBar>(hud,"enemyHealthFill"));AddFill(b,"enemyManaFill",Field<HUDResourceBar>(hud,"enemyManaFill"));
        return b.ToString();
    }

    static T Field<T>(object target,string name) where T:class => typeof(PaperBattleHUD).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(target) as T;
    static void AddText(StringBuilder b,string name,TMP_Text text)=>b.AppendLine($"{name}: text='{text?.text}' active={text!=null&&text.gameObject.activeInHierarchy} enabled={text!=null&&text.enabled} color={text?.color} font={text?.font?.name} rect={Rect(text?.rectTransform)} sibling={text?.transform.GetSiblingIndex()}");
    static void AddImage(StringBuilder b,string name,Image image)
    {
        Mask mask=image!=null?image.GetComponentInParent<Mask>():null;
        b.AppendLine($"{name}: sprite={image?.sprite?.name} active={image!=null&&image.gameObject.activeInHierarchy} enabled={image!=null&&image.enabled} color={image?.color} type={image?.type} rect={Rect(image?.rectTransform)} mask={mask!=null} showMask={mask!=null&&mask.showMaskGraphic}");
    }
    static void AddFill(StringBuilder b,string name,HUDResourceBar fill)=>b.AppendLine($"{name}: amount={fill?.FillAmount:F4} active={fill!=null&&fill.gameObject.activeInHierarchy} rect={Rect(fill?.rectTransform)} pivot={fill?.rectTransform.pivot}");
    static string Rect(RectTransform rect)
    {
        if(rect==null)return "null";var corners=new Vector3[4];rect.GetWorldCorners(corners);Camera camera=rect.GetComponentInParent<Canvas>()?.worldCamera;
        Vector2 lo=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),hi=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);return $"({lo.x:F1},{lo.y:F1})-({hi.x:F1},{hi.y:F1})";
    }
}
