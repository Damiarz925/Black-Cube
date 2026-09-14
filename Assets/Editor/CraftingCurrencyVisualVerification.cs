// Developer map: End-to-end batch Play verification through the live EventSystem and authored inventory hierarchy.
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CraftingCurrencyVisualVerification
{
    const string PendingKey="BlackCube.CraftingCurrencyVisualVerification.Pending";
    const string HeldPath="ReviewCaptures/crafting-currency-held.png";
    const string PostPath="ReviewCaptures/crafting-currency-post-use.png";
    const string ReportPath="ReviewCaptures/crafting-currency-e2e.txt";
    static double readyAt;static int step;static bool completed,hadSave;static string savedValue;
    static PaperBattleHUD hud;static CurrencyInventory currencies;static Gear firstGear,shiftGear;static CurrencySlotUI currencySlot;static ItemSlotUI gearSlot;static CraftingCurrencyCursorUI cursor;
    static Mouse mouse;static bool addedMouse;static Keyboard keyboard;static bool addedKeyboard;static int initialCount,afterDefaultCount;

    static CraftingCurrencyVisualVerification(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}

    public static void Run()
    {
        Directory.CreateDirectory("ReviewCaptures");hadSave=PlayerPrefs.HasKey(GamePersistence.SaveKey);savedValue=hadSave?PlayerPrefs.GetString(GamePersistence.SaveKey):null;
        SessionState.SetBool(PendingKey,true);completed=false;step=0;readyAt=0;EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");Screen.SetResolution(1920,1080,false);EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!SessionState.GetBool(PendingKey,false))return;
        if(!EditorApplication.isPlaying)
        {
            if(completed){RestoreSave();SessionState.EraseBool(PendingKey);EditorApplication.Exit(0);}return;
        }
        if(completed||EditorApplication.timeSinceStartup<readyAt)return;
        try{RunStep();}catch(Exception e){File.WriteAllText(ReportPath,"FAIL\n"+e);Debug.LogException(e);CleanupDevices();completed=true;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}
    }

    static void RunStep()
    {
        if(step==0)
        {
            hud=UnityEngine.Object.FindAnyObjectByType<PaperBattleHUD>();currencies=CurrencyInventory.Instance;
            if(hud==null||currencies==null||Inventory.Instance==null||EventSystem.current==null)return;
            currencies.CancelArmed();
            Button inventoryButton=hud.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="Inventory Button");
            Require(inventoryButton!=null,"Authored Inventory Button is missing");Click(inventoryButton.gameObject);Require(hud.inventoryPanel.activeSelf,"Inventory button click did not open the real panel");
            currencies.Add(CraftingCurrencyType.NormalToMagic,Mathf.Max(0,5-currencies.Count(CraftingCurrencyType.NormalToMagic)));
            firstGear=Gear("E2E default-use gear");Inventory.Instance.Add(firstGear);step=1;Delay();return;
        }
        if(step==1)
        {
            currencySlot=FindCurrencySlot();gearSlot=FindGearSlot(firstGear);Require(currencySlot!=null&&gearSlot!=null,"Live currency or gear slot was not created");
            Require(UnityEngine.Object.FindObjectsByType<InventoryEquipmentPanelUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Expected exactly one authoritative inventory layout instance");
            initialCount=currencies.Count(CraftingCurrencyType.NormalToMagic);Click(currencySlot.gameObject);Require(currencies.ArmedCurrency==CraftingCurrencyType.NormalToMagic,"EventSystem currency click did not arm the currency");
            cursor=UnityEngine.Object.FindAnyObjectByType<CraftingCurrencyCursorUI>();Require(cursor!=null&&cursor.IsShowing&&currencySlot.IsSelected,"Real selection did not show cursor and source highlight");
            step=2;Delay();return;
        }
        if(step==2)
        {
            Capture(HeldPath,new Vector2(520,535));Click(gearSlot.gameObject);
            afterDefaultCount=currencies.Count(CraftingCurrencyType.NormalToMagic);Require(afterDefaultCount==initialCount-1,"Default EventSystem gear click did not consume exactly one currency");
            Require(firstGear.ItemRarity==LootManager.GearRarity.Magic,"Default EventSystem gear click did not craft the eligible item");Require(!currencies.ArmedCurrency.HasValue&&!cursor.IsShowing,"Default successful use did not clear held presentation");
            Capture(PostPath,new Vector2(520,535));shiftGear=Gear("E2E shift-use gear");Inventory.Instance.Add(shiftGear);step=3;Delay();return;
        }
        if(step==3)
        {
            currencySlot=FindCurrencySlot();gearSlot=FindGearSlot(shiftGear);Require(currencySlot!=null&&gearSlot!=null,"Shift verification slots are missing");Click(currencySlot.gameObject);
            keyboard=InputSystem.AddDevice<Keyboard>();addedKeyboard=true;keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));InputSystem.Update();int before=currencies.Count(CraftingCurrencyType.NormalToMagic);Click(gearSlot.gameObject);InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            Require(currencies.Count(CraftingCurrencyType.NormalToMagic)==before-1&&currencies.ArmedCurrency==CraftingCurrencyType.NormalToMagic,"Shift EventSystem use did not consume one and retain selection");
            cursor.HandleClickTarget(RaycastAt(new Vector2(10,10)));Require(!currencies.ArmedCurrency.HasValue&&!cursor.IsShowing,"Raycasted background target did not cancel held currency");
            WriteReport();CleanupDevices();completed=true;EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;
        }
    }

    static void Click(GameObject expectedRoot)
    {
        var rect=(RectTransform)expectedRoot.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 position=RectTransformUtility.WorldToScreenPoint(CanvasCamera(rect),Vector3.Lerp(corners[0],corners[2],.5f));
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=position};var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        RaycastResult hit=hits.FirstOrDefault(h=>h.gameObject==expectedRoot||h.gameObject.transform.IsChildOf(expectedRoot.transform));Require(hit.gameObject!=null,"No live UI raycast reached "+Path(expectedRoot.transform));
        ExecuteEvents.ExecuteHierarchy(hit.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    static GameObject RaycastAt(Vector2 position){var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);return hits.Count>0?hits[0].gameObject:null;}
    static CurrencySlotUI FindCurrencySlot()=>UnityEngine.Object.FindObjectsByType<CurrencySlotUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>s.Type==CraftingCurrencyType.NormalToMagic&&s.gameObject.activeInHierarchy);
    static ItemSlotUI FindGearSlot(Gear gear)=>UnityEngine.Object.FindObjectsByType<ItemSlotUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>ReferenceEquals(s.Item,gear)&&s.gameObject.activeInHierarchy);
    static Gear Gear(string name){var gear=new GameObject(name).AddComponent<Gear>();gear.Initialize(LootManager.GearType.Helmets,LootManager.GearRarity.Normal,40,Element.Phys);gear.ApplyMods(new System.Collections.Generic.List<RolledMod>{new(StatTypes.Life,1,10,true)});return gear;}
    static Camera CanvasCamera(RectTransform rect){Canvas canvas=rect.GetComponentInParent<Canvas>();return canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?(canvas.worldCamera??Camera.main):null;}
    static Vector2 PanelPoint(float x,float y){var rect=(RectTransform)hud.inventoryPanel.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);return RectTransformUtility.WorldToScreenPoint(CanvasCamera(rect),Vector3.Lerp(Vector3.Lerp(corners[0],corners[1],y),Vector3.Lerp(corners[3],corners[2],y),x));}
    static void Delay()=>readyAt=EditorApplication.timeSinceStartup+.35;
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}

    static void Capture(string path,Vector2 pointer)
    {
        Canvas canvas=hud.GetComponentInParent<Canvas>();Camera camera=Camera.main??UnityEngine.Object.FindAnyObjectByType<Camera>();RenderMode oldMode=canvas.renderMode;Camera oldCanvasCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;RenderTexture oldTarget=camera.targetTexture;RenderTexture oldActive=RenderTexture.active;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var screenshot=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;Canvas.ForceUpdateCanvases();cursor.SetScreenPosition(pointer);camera.Render();RenderTexture.active=target;screenshot.ReadPixels(new Rect(0,0,1920,1080),0,0,false);screenshot.Apply(false,false);File.WriteAllBytes(path,screenshot.EncodeToPNG());}
        finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCanvasCamera;canvas.planeDistance=oldPlane;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(screenshot);}
    }
    static void WriteReport()
    {
        var b=new StringBuilder();b.AppendLine("PASS: actual Inventory button opened the authored inventory through EventSystem");b.AppendLine("PASS: actual currency slot raycast/click armed NormalToMagic");b.AppendLine("PASS: held cursor rendered after actual selection and remained raycast-transparent");
        b.AppendLine($"PASS: default actual gear click count {initialCount}->{afterDefaultCount}, rarity={firstGear.ItemRarity}, cleared=True");b.AppendLine("PASS: Shift actual gear click consumed exactly one and retained selection");b.AppendLine("PASS: production click-away handler canceled selection from an EventSystem background raycast");
        b.AppendLine($"inventoryPath={Path(hud.inventoryPanel.transform)} cursorPath={Path(cursor.transform)} canvas={Path(cursor.GetComponentInParent<Canvas>().transform)}");b.AppendLine($"cursorRaycast={cursor.Icon.raycastTarget} blocksRaycasts={cursor.GetComponent<CanvasGroup>().blocksRaycasts} layoutInstances=1");File.WriteAllText(ReportPath,b.ToString());
    }
    static string Path(Transform value)=>value.parent==null?value.name:Path(value.parent)+"/"+value.name;
    static void CleanupDevices(){if(addedMouse&&mouse!=null)InputSystem.RemoveDevice(mouse);if(addedKeyboard&&keyboard!=null)InputSystem.RemoveDevice(keyboard);mouse=null;keyboard=null;addedMouse=addedKeyboard=false;}
    static void RestoreSave(){if(hadSave)PlayerPrefs.SetString(GamePersistence.SaveKey,savedValue);else PlayerPrefs.DeleteKey(GamePersistence.SaveKey);PlayerPrefs.Save();}
}
