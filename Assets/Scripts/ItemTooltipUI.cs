using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A stationary, enterable hover card. Item data, not actor totals, drives its contents.</summary>
public class ItemTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    RectTransform owner;
    Gear item;
    bool equipped;
    TMP_Text heading, body, actionLabel;
    Button scrapButton;
    ScrollRect scroll;
    PointerEventData pointer;
    public Button ScrapButton => scrapButton;
    public string BodyText => body.text;

    public static string ElementName(Element element) => element switch
    { Element.Phys => "Physical", Element.Light => "Lightning", _ => element.ToString() };
    public static Color ElementTint(Element element) => element switch
    { Element.Fire => new Color(1,.45f,.2f), Element.Cold => new Color(.35f,.8f,1), Element.Light => new Color(1,.9f,.3f), Element.Poison => new Color(.4f,1,.3f), Element.Void => new Color(.75f,.4f,1), _ => new Color(.85f,.87f,.9f) };

    public static string Describe(Gear item)
    {
        if(item.IsScrap) return $"Material\nStack count: {item.StackCount}\n\nCannot be equipped or dismantled.\nCrafting is not available yet.";
        var s = new StringBuilder($"ITEM LEVEL {item.ItemLevel}\n");
        if(item.ItemType == LootManager.GearType.Weapons)
        {
            s.AppendLine($"\nBASE ELEMENT: {ElementName(item.BaseElement)}");
            s.AppendLine($"{ElementName(item.BaseElement)} damage: {item.GetEffectiveBaseDamage():0.##}");
            s.AppendLine($"Attacks / second: {item.GetEffectiveAttackSpeed():0.##}");
            s.AppendLine($"Weapon-only critical chance: {item.GetEffectiveBaseCrit()*100:0.##}%");
            s.AppendLine("\nWEAPON BASE / LOCAL MODIFIERS");
            s.AppendLine($"Base damage: {item.BaseDamage:0.##}");
            s.AppendLine($"Base attacks / second: {item.BaseAttackSpeed:0.##}");
            s.AppendLine($"Base critical chance: {item.BaseCritChance*100:0.##}%");
            Local(s,"Added local damage",item.LocalFlatDamage,false);
            Local(s,"Increased local damage",item.LocalIncDamage,true);
            Local(s,"Added local critical chance",item.LocalBaseCrit,true);
            Local(s,"Increased local critical chance",item.LocalIncCrit,true);
            Local(s,"Increased local attack speed",item.LocalIncAttackSpeed,true);
        }
        s.AppendLine("\nEQUIPPED MODIFIERS / STATUS EFFECTS");
        if(item.globalRolledMods.Count == 0) s.AppendLine("None");
        foreach(var mod in item.globalRolledMods)
            s.AppendLine($"{StatDisplayFormatting.ToFriendlyName(mod.statType)}: {mod.value:+0.##;-0.##;0}{(StatsComponent.IsPercentStat(mod.statType)?"%":"")}");
        s.Append("\nBase element is damage type, not an applied status.");
        return s.ToString();
    }
    static void Local(StringBuilder s,string label,float value,bool percent)
    { if(value != 0) s.AppendLine($"{label}: {(percent?value*100:value):+0.##;-0.##;0}{(percent?"%":"")}"); }

    public static ItemTooltipUI Create(Transform canvas)
    {
        var go = Box(canvas,"Item details",new Color(.025f,.028f,.035f,1));
        var tip = go.AddComponent<ItemTooltipUI>();
        var layer = go.AddComponent<Canvas>(); layer.overrideSorting=true; layer.sortingOrder=90;
        go.AddComponent<GraphicRaycaster>();
        var rect = (RectTransform)go.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(0,1); rect.sizeDelta=new Vector2(420,540);
        tip.heading = Label(go.transform,20); Place(tip.heading.rectTransform,0,.88f,1,1,16);
        var view = Box(go.transform,"Scrollable item stats",new Color(0,0,0,0));
        Place((RectTransform)view.transform,0,.15f,1,.88f,16);
        tip.scroll=view.AddComponent<ScrollRect>(); tip.scroll.horizontal=false; tip.scroll.scrollSensitivity=30; tip.scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport = Box(view.transform,"Viewport",Color.clear); Place((RectTransform)viewport.transform,0,0,1,1,0); viewport.AddComponent<RectMask2D>();
        tip.body=Label(viewport.transform,15);
        var content=tip.body.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;
        tip.body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        tip.scroll.viewport=(RectTransform)viewport.transform;tip.scroll.content=content;
        var action=Box(go.transform,"Scrap action",new Color(.28f,.16f,.11f));Place((RectTransform)action.transform,0,0,1,.14f,12);
        tip.scrapButton=action.AddComponent<Button>();tip.scrapButton.targetGraphic=action.GetComponent<Image>();
        tip.actionLabel=Label(action.transform,16);Place(tip.actionLabel.rectTransform,0,0,1,1,4);tip.actionLabel.alignment=TextAlignmentOptions.Center;
        tip.scrapButton.onClick.AddListener(tip.Dismantle);
        go.SetActive(false);return tip;
    }
    public void Show(ItemSlotUI slot)
    {
        if(slot == null || slot.Item == null)return;
        Show(slot.Item, (RectTransform)slot.transform, false);
    }
    public void Show(Gear gear, RectTransform anchor, bool isEquipped, PointerEventData data = null)
    {
        if (gear == null || anchor == null) return;
        bool same=owner==anchor && item==gear && gameObject.activeSelf;
        owner=anchor;item=gear;equipped=isEquipped;pointer=data;gameObject.SetActive(true);
        heading.text=item.IsScrap ? $"SCRAP  x{item.StackCount}" : $"{item.ItemRarity} {ItemSlotUI.DisplayType(item.ItemType)}";
        heading.color=ItemSlotUI.RarityColor(item.ItemRarity);
        body.text=Describe(item);
        scrapButton.interactable=!equipped && Inventory.Instance != null && Inventory.Instance.CanDismantle(item);
        actionLabel.text=equipped ? "EQUIPPED / CANNOT SCRAP" : item.IsScrap ? "SCRAP / MATERIAL ONLY" : Inventory.ScrapYield(item)>0 ? $"SCRAP ITEM  /  +{Inventory.ScrapYield(item)} SCRAP" : "NO DISMANTLE YIELD CONFIGURED";
        if(!same)
        {
            scroll.verticalNormalizedPosition=1;
            var corners=new Vector3[4];owner.GetWorldCorners(corners);
            var parent=(RectTransform)transform.parent;var point=parent.InverseTransformPoint(corners[2]);
            var r=(RectTransform)transform;
            r.sizeDelta=new Vector2(Mathf.Min(420,parent.rect.width-24),Mathf.Min(540,parent.rect.height-24));
            // Touch the source edge so transfer needs no timed linger or broad corridor.
            float x=point.x;
            if(x+r.rect.width>parent.rect.xMax-12)x=parent.InverseTransformPoint(corners[1]).x-r.rect.width;
            x=Mathf.Clamp(x,parent.rect.xMin+12,parent.rect.xMax-r.rect.width-12);
            float y=Mathf.Clamp(point.y,parent.rect.yMin+r.rect.height+12,parent.rect.yMax-12);
            r.localPosition=new Vector3(x,y,0);
        }
    }
    public void LeaveSlot(ItemSlotUI slot) { if(owner==slot.transform) Hide(); }
    public void Leave(RectTransform anchor, PointerEventData data)
    { if(owner==anchor) { pointer=data; if(!Contains((RectTransform)transform,data)) Hide(); } }
    static bool Contains(RectTransform rect, PointerEventData data) =>
        RectTransformUtility.RectangleContainsScreenPoint(rect,data.position,data.enterEventCamera);
    public void OnPointerEnter(PointerEventData data){pointer=data;}
    public void OnPointerExit(PointerEventData data)
    { pointer=data; if(owner==null || !Contains(owner,data)) Hide(); }
    void LateUpdate()
    {
        if(owner==null || !owner.gameObject.activeInHierarchy || item==null
            || (equipped && EquipmentManager.Instance?.GetEquipped(item.ItemType)!=item)) { Hide(); return; }
        // EventSystem updates this same pointer object. Recheck after scrolling/layout changes.
        if(pointer!=null && !Contains(owner,pointer) && !Contains((RectTransform)transform,pointer)) Hide();
    }
    void Dismantle()
    {
        scrapButton.interactable=false;
        if(!equipped && Inventory.Instance != null) Inventory.Instance.TryDismantle(item);
        Hide();
    }
    public void Hide(){owner=null;item=null;pointer=null;gameObject.SetActive(false);}
    static GameObject Box(Transform parent,string name,Color tint)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=tint;return go;}
    static TMP_Text Label(Transform parent,int size)
    {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var t=go.GetComponent<TextMeshProUGUI>();t.fontSize=size;t.raycastTarget=false;return t;}
    static void Place(RectTransform r,float x0,float y0,float x1,float y1,float pad)
    {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=new Vector2(pad,pad);r.offsetMax=new Vector2(-pad,-pad);}
}
