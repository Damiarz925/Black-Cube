// Developer map: Formats actual local/global item values and owns hover-card scrolling/placement. Revalidates ownership before scrapping and closes when the item or anchor disappears.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Text;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One deterministic formatter shared by inventory, equipped, enemy and relic item views.</summary>
public static class ItemTooltipFormatter
{
    public const string Divider = "────────────────────────";

    public static string DescribeGear(Gear item)
    {
        if (item == null) return string.Empty;
        if (item.IsScrap) return $"<color=#BFA86A><b>MATERIAL</b></color>\n\nStack Count: {item.StackCount}\nCannot be equipped or crafted.";
        var s = new StringBuilder();
        s.AppendLine($"<color=#85898F>{ItemSlotUI.DisplayType(item.ItemType).ToUpperInvariant()}  •  ITEM LEVEL {item.ItemLevel}</color>");
        s.AppendLine($"<color=#8DC9D8><b>CRAFTING POTENTIAL: {item.CurrentCraftingPotential} / {item.MaximumCraftingPotential}</b></color>  <color=#85898F>ORIGIN {item.OriginRarity.ToString().ToUpperInvariant()}</color>");
        if (item.ItemType == LootManager.GearType.Weapons)
        {
            s.AppendLine();
            item.GetEffectiveBaseDamageRange(out float minimum,out float maximum);
            s.AppendLine($"<b>{ItemTooltipUI.ElementName(item.BaseElement)} Damage:</b> {minimum:0.##}–{maximum:0.##}  <color=#85898F>(Average {(minimum+maximum)*.5f:0.##})</color>");
            s.AppendLine($"<b>Crit Chance:</b> {item.GetEffectiveBaseCrit()*100:0.##}%");
            s.AppendLine($"<b>Attacks Per Second:</b> {item.GetEffectiveAttackSpeed():0.0#}");
            s.AppendLine($"<b>Average Weapon DPS:</b> {item.GetAverageWeaponDps():0.##}  <color=#85898F>(local damage × local APS; Crit excluded)</color>");
        }
        var mods = new List<RolledMod>();
        foreach (var mod in item.rolledMods)
            if (mod != null && !Gear.IsWeaponBaseStat(mod.statType)) mods.Add(mod);
        mods.Sort(Compare);
        s.AppendLine();
        s.AppendLine("<color=#9FC8BC><b>IMPLICIT</b></color>");
        RolledMod implicitMod=item.ImplicitMod;
        if (implicitMod != null) AppendMod(s,item,implicitMod,true);
        else s.AppendLine("<color=#85898F>—</color>");
        s.AppendLine(); s.AppendLine($"<color=#555A63>{Divider}</color>");
        foreach (var side in new[]{AffixSide.Prefix,AffixSide.Suffix})
        {
            s.AppendLine($"<b>{(side==AffixSide.Prefix?"PREFIXES":"SUFFIXES")}</b>");
            bool any=false;
            foreach (var mod in mods)
            {
                if(mod.lockedOriginal || AffixPolicy.Side(mod)!=side)continue;
                any=true;
                AppendMod(s,item,mod,false);
            }
            if(!any)s.AppendLine("<color=#85898F>—</color>");
        }
        return s.ToString().TrimEnd();
    }

    static void AppendMod(StringBuilder s,Gear item,RolledMod mod,bool implicitLine)
    {
        SpecialAffixDefinition special=mod.isBossSpecial?BossSpecialCatalog.Find(mod.specialPoolId,mod.specialModifierId):null;
        bool percent=StatsComponent.IsPercentStat(mod.statType);
        bool paired=item.IsLocalAffix(mod.statType) && (mod.statType is StatTypes.FlatPhys
            or StatTypes.FlatFire or StatTypes.FlatCold or StatTypes.FlatLight or StatTypes.FlatVoid);
        string unit=percent?"%":"";
        string rolled=special!=null?$"{special.displayName}: {special.description}":paired?$"Adds {mod.value:0.##}–{mod.HighValue:0.##} {ItemTooltipUI.ElementName(item.BaseElement)} Damage"
            :$"{StatDisplayFormatting.ToFriendlyName(mod.statType)}: {mod.value:+0.##;-0.##;0}{unit}";
        string range=TierRange(item,mod,percent);
        string color=implicitLine?"#9FC8BC":mod.isEmpowered?"#73D8EE":mod.isBossSpecial?"#D88BFF":"#E4C979";
        // Reserve a compact space for the runtime-built padlock Image. TMP's
        // shipped font does not contain the Unicode lock emoji.
        string prefix=implicitLine?"   ":"";
        string local=item.IsLocalAffix(mod.statType)?"  <color=#85898F>LOCAL</color>":"";
        string rank=mod.isEmpowered?"EMPOWERED":mod.isBossSpecial?$"APEX — {BossSpecialCatalog.SourceName(mod.specialPoolId)}":$"T{mod.tierIndex}";
        s.AppendLine($"<color={color}>{prefix}<b>{rolled}</b></color>  <color=#85898F>{range} {rank}</color>{local}");
    }

    static string TierRange(Gear item,RolledMod mod,bool percent)
    {
        AffixDefinitions def=ModManager.Instance?.Database?.GetDefinition(mod.statType);
#if UNITY_EDITOR
        if(def==null)def=UnityEditor.AssetDatabase.LoadAssetAtPath<ModDatabase>(
            "Assets/Prefabs/Scriptable Objects/ModDatabase.asset")?.GetDefinition(mod.statType);
#endif
        if(mod.isBossSpecial)return "(special pool roll)";
        var tiers=ModManager.ApplicableTiers(def,item.ItemType,item.WeaponTypeId);
        var tier=tiers.Find(t=>t.tierIndex==mod.tierIndex);
        if(tier==null)return "(historical tier / range unavailable)";
        float min=tier.minValue,max=tier.maxValue,minHigh=tier.minHighValue,maxHigh=tier.maxHighValue;
        if(mod.isEmpowered)EmpowermentCrafting.EmpoweredRange(def,tier,out min,out max,out minHigh,out maxHigh);
        bool valid=mod.value>=min-.011f&&mod.value<=max+.011f
            && (!mod.hasSecondaryValue || !tier.pairedDamage || mod.HighValue>=minHigh-.011f
                && mod.HighValue<=maxHigh+.011f);
        if(!valid)return "(historical roll / current range differs)";
        string unit=percent?"%":"";
        return tier.pairedDamage?$"(min {min:0.##}–{max:0.##}, max {minHigh:0.##}–{maxHigh:0.##})"
            :$"({min:0.##}–{max:0.##}{unit})";
    }

    public static string DescribeRelic(RelicData relic)
    {
        if (relic == null) return string.Empty;
        var s = new StringBuilder($"<b>{relic.rarity} RELIC</b>  <color=#85898F>•  RELIC LEVEL {relic.relicLevel}  •  CYCLE {relic.cycle}</color>\n");
        s.AppendLine(relic.craftableThisCycle ? "<color=#B88AE0>CURRENT CYCLE / CRAFTABLE</color>" : "<color=#85898F>PAST CYCLE / LOCKED</color>");
        s.AppendLine($"<color=#555A63>{Divider}</color>");
        s.AppendLine("<b>RELIC MODIFIERS</b>");
        var mods = new List<RelicModifier>(relic.modifiers ?? new List<RelicModifier>());
        mods.Sort((a,b)=>RelicPriority(a.type).CompareTo(RelicPriority(b.type)));
        foreach (var mod in mods)
        {
            if(mod==null)continue;
            var definition=RelicModifierDefinitions.Get(mod.type);
            string label=definition?.Label??mod.type.ToString();
            string unit=definition==null||definition.Percent?"%":"";
            var tier=definition?.Tier(mod.tierIndex);
            string range=tier==null?"LEGACY / preserved":tier.FixedValue?$"{tier.Minimum:0.##}{unit}":$"{tier.Minimum:0.##}–{tier.Maximum:0.##}{unit}";
            string tierText=tier==null?"LEGACY":$"T{tier.TierIndex}";
            s.AppendLine($"{label}: <b>{mod.value:+0.##;-0.##;0}{unit}</b>  ({range}) {tierText}  {(mod.lockedOriginal ? "[LOCKED]" : "[CRAFTABLE]")}");
        }
        return s.ToString().TrimEnd();
    }

    public static int Compare(RolledMod a, RolledMod b)
    {
        int side=AffixPolicy.Side(a.statType).CompareTo(AffixPolicy.Side(b.statType));
        if(side!=0)return side;
        int group = Group(a.statType).CompareTo(Group(b.statType));
        if (group != 0) return group;
        int canonical = OrderWithinGroup(a.statType).CompareTo(OrderWithinGroup(b.statType));
        if (canonical != 0) return canonical;
        int tier = a.tierIndex.CompareTo(b.tierIndex);
        return tier != 0 ? tier : a.value.CompareTo(b.value);
    }

    static int Group(StatTypes type)
    {
        var category = StatCategoryMapping.GetCategory(type);
        if (category is StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage or StatCategory.Penetration) return 0;
        if (type is StatTypes.AttackSpeed or StatTypes.CritChance or StatTypes.CritMult or StatTypes.BaseCritChance or StatTypes.Accuracy or StatTypes.ChanceToHitTwice) return 1;
        if (category is StatCategory.DamageOverTime or StatCategory.Ailments) return 2;
        if (type is StatTypes.LifeRegeneration or StatTypes.ManaRegeneration or StatTypes.LifeOnHit or StatTypes.ManaOnHit or StatTypes.LifeOnKill or StatTypes.ManaOnKill or StatTypes.CooldownRecovery) return 4;
        if (category is StatCategory.Defenses or StatCategory.Resources) return 3;
        return 5;
    }
    static int OrderWithinGroup(StatTypes type)=>type switch
    {
        StatTypes.FlatPhys or StatTypes.PhysDmg or StatTypes.PhysMult or StatTypes.PhysPenetration=>0,
        StatTypes.FlatFire or StatTypes.FireDmg or StatTypes.FireMult or StatTypes.FirePenetration=>10,
        StatTypes.FlatCold or StatTypes.ColdDmg or StatTypes.ColdMult or StatTypes.ColdPenetration=>20,
        StatTypes.FlatLight or StatTypes.LightDmg or StatTypes.LightMult or StatTypes.LightPenetration=>30,
        StatTypes.FlatVoid or StatTypes.VoidDmg or StatTypes.VoidMult or StatTypes.VoidPenetration=>40,
        _=>100+(int)type
    };
    static int RelicPriority(RelicModifierType type)=>type switch{RelicModifierType.MoreDamage=>0,RelicModifierType.MoreAttackSpeed=>1,_=>2};
}

/// <summary>A stationary, enterable hover card. Item data, not actor totals, drives its contents.</summary>
public class ItemTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    RectTransform owner;
    Gear item;
    bool equipped;
    bool validatePlayerEquipment;
    TMP_Text heading, body, actionLabel;
    Image implicitLockIcon;
    Button scrapButton;
    ScrollRect scroll;
    PointerEventData pointer;
    public Button ScrapButton => scrapButton;
    public string BodyText => body.text;
    public Image ImplicitLockIcon => implicitLockIcon;

    public static string ElementName(Element element) => element switch
    { Element.Phys => "Physical", Element.Light => "Lightning", _ => element.ToString() };
    public static Color ElementTint(Element element) => element switch
    { Element.Fire => new Color(1,.45f,.2f), Element.Cold => new Color(.35f,.8f,1), Element.Light => new Color(1,.9f,.3f), Element.Poison => new Color(.4f,1,.3f), Element.Void => new Color(.75f,.4f,1), _ => new Color(.85f,.87f,.9f) };

    public static string Describe(Gear item)=>ItemTooltipFormatter.DescribeGear(item);
    static string GroupFor(StatTypes type) => StatCategoryMapping.GetCategory(type) switch
    {
        StatCategory.WeaponBase => "WEAPON / BASE",
        StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage or StatCategory.Penetration => "DAMAGE",
        StatCategory.DamageOverTime or StatCategory.Ailments => "AILMENTS / STATUS",
        StatCategory.Defenses => "DEFENSES",
        StatCategory.Resources => "RESOURCES",
        StatCategory.Utility => "UTILITY / SPEED",
        StatCategory.Attributes => "ATTRIBUTES / SCALING",
        _ => "OTHER"
    };
    static string HeaderColor(string group) => group switch
    {
        "WEAPON / BASE" => "#C5A66A",
        "DAMAGE" => "#C96B6E",
        "AILMENTS / STATUS" => "#C96B6E",
        "DEFENSES" => "#7E9EAD",
        "RESOURCES" => "#7E9EAD",
        "ATTRIBUTES / SCALING" => "#C5A66A",
        _ => "#9B9DA1"
    };
    static void Header(StringBuilder s, string label, string color)
    {
        if (s.Length > 0 && s[s.Length - 1] != '\n') s.AppendLine();
        s.Append($"\n<color={color}><b>{label}</b></color>\n");
    }
    static void Local(StringBuilder s,string label,float value,bool percent)
    { if(value != 0) s.AppendLine($"{label}: {(percent?value*100:value):+0.##;-0.##;0}{(percent?"%":"")}"); }

    public static ItemTooltipUI Create(Transform canvas)
    {
        var go = Box(canvas,"Item details",new Color(.025f,.028f,.035f,1));
        var tip = go.AddComponent<ItemTooltipUI>();
        var layer = go.AddComponent<Canvas>(); layer.overrideSorting=true; layer.sortingOrder=90;
        go.AddComponent<GraphicRaycaster>();
        var rect = (RectTransform)go.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(0,1); rect.sizeDelta=new Vector2(440,420);
        tip.heading = Label(go.transform,18); Place(tip.heading.rectTransform,0,.87f,1,1,12);
        var view = Box(go.transform,"Scrollable item stats",new Color(0,0,0,0));
        Place((RectTransform)view.transform,0,.13f,1,.87f,12);
        tip.scroll=view.AddComponent<ScrollRect>(); tip.scroll.horizontal=false; tip.scroll.scrollSensitivity=30; tip.scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport = Box(view.transform,"Viewport",Color.clear); Place((RectTransform)viewport.transform,0,0,1,1,0); viewport.AddComponent<RectMask2D>();
        tip.body=Label(viewport.transform,12);
        tip.body.textWrappingMode = TextWrappingModes.Normal;
        tip.body.lineSpacing = -5f;
        var content=tip.body.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;
        tip.body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var lockObject=new GameObject("Permanent implicit padlock",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        lockObject.transform.SetParent(tip.body.transform,false);
        var lockRect=(RectTransform)lockObject.transform;
        lockRect.anchorMin=lockRect.anchorMax=new Vector2(0,1);
        lockRect.pivot=new Vector2(.5f,.5f);
        lockRect.sizeDelta=new Vector2(16,16);
        tip.implicitLockIcon=lockObject.GetComponent<Image>();
        tip.implicitLockIcon.sprite=TooltipLockIcon.IconSprite;
        tip.implicitLockIcon.raycastTarget=false;
        tip.scroll.viewport=(RectTransform)viewport.transform;tip.scroll.content=content;
        var action=Box(go.transform,"Scrap action",new Color(.28f,.16f,.11f));Place((RectTransform)action.transform,0,.01f,1,.12f,10);
        tip.scrapButton=action.AddComponent<Button>();tip.scrapButton.targetGraphic=action.GetComponent<Image>();
        tip.actionLabel=Label(action.transform,13);Place(tip.actionLabel.rectTransform,0,0,1,1,4);tip.actionLabel.alignment=TextAlignmentOptions.Center;
        tip.scrapButton.onClick.AddListener(tip.Dismantle);
        go.SetActive(false);return tip;
    }
    public void Show(ItemSlotUI slot)
    {
        if(slot == null || slot.Item == null)return;
        Show(slot.Item, (RectTransform)slot.transform, false);
    }
    public void Show(Gear gear, RectTransform anchor, bool isEquipped, PointerEventData data = null,
        bool requirePlayerEquipment = true)
    {
        if (gear == null || anchor == null) return;
        bool same=owner==anchor && item==gear && gameObject.activeSelf;
        owner=anchor;item=gear;equipped=isEquipped;validatePlayerEquipment=requirePlayerEquipment;pointer=data;gameObject.SetActive(true);
        RefreshVisibleContent();
        if(!same)
        {
            scroll.verticalNormalizedPosition=1;
            var corners=new Vector3[4];owner.GetWorldCorners(corners);
            var parent=(RectTransform)transform.parent;var point=parent.InverseTransformPoint(corners[2]);
            var r=(RectTransform)transform;
            float width=Mathf.Min(440,parent.rect.width-24);
            float preferredBody=body.GetPreferredValues(body.text,width-24,0).y;
            float height=Mathf.Clamp(preferredBody+116,210,Mathf.Min(540,parent.rect.height-24));
            r.sizeDelta=new Vector2(width,height);
            // Touch the source edge so transfer needs no timed linger or broad corridor.
            float x=point.x;
            if(x+r.rect.width>parent.rect.xMax-12)x=parent.InverseTransformPoint(corners[1]).x-r.rect.width;
            x=Mathf.Clamp(x,parent.rect.xMin+12,parent.rect.xMax-r.rect.width-12);
            float y=Mathf.Clamp(point.y,parent.rect.yMin+r.rect.height+12,parent.rect.yMax-12);
            r.localPosition=new Vector3(x,y,0);
        }
    }
    void OnEnable(){CurrencyInventory.GearChanged+=OnGearChanged;if(Inventory.Instance!=null)Inventory.Instance.OnInventoryChanged+=ValidateTarget;}
    void OnDisable(){CurrencyInventory.GearChanged-=OnGearChanged;if(Inventory.Instance!=null)Inventory.Instance.OnInventoryChanged-=ValidateTarget;}
    void OnGearChanged(Gear changed){if(item!=changed)return;if(!TargetExists()){Hide();return;}RefreshVisibleContent();ResizeInPlace();}
    void ValidateTarget(){if(item!=null&&!TargetExists())Hide();}
    bool TargetExists()
    {
        if(item==null||owner==null)return false;
        if(equipped&&validatePlayerEquipment)return EquipmentManager.Instance?.GetEquipped(item.ItemType)==item;
        if(!equipped)return Inventory.Instance!=null&&Inventory.Instance.Items.Contains(item);
        return true;
    }
    void RefreshVisibleContent(){if(item==null)return;heading.text=item.IsScrap?$"SCRAP  x{item.StackCount}":$"{item.ItemRarity} {ItemSlotUI.DisplayType(item.ItemType)}";heading.color=ItemSlotUI.RarityColor(item.ItemRarity);body.text=Describe(item);body.ForceMeshUpdate();PlaceImplicitLock();scrapButton.interactable=!equipped&&Inventory.Instance!=null&&Inventory.Instance.CanDismantle(item);actionLabel.text=equipped?"EQUIPPED / CANNOT SCRAP":item.IsScrap?"SCRAP / MATERIAL ONLY":item.ItemRarity==LootManager.GearRarity.Normal?"DISMANTLE / NO FRAGMENTS":$"DISMANTLE / +{Inventory.ScrapYield(item)} {(item.ItemRarity==LootManager.GearRarity.Magic?"NORMAL→MAGIC":"MAGIC→RARE")} FRAGMENT{(Inventory.ScrapYield(item)==1?"":"S")}";}
    void ResizeInPlace(){var r=(RectTransform)transform;var parent=(RectTransform)transform.parent;float width=Mathf.Min(440,parent.rect.width-24);float preferred=body.GetPreferredValues(body.text,width-24,0).y;r.sizeDelta=new Vector2(width,Mathf.Clamp(preferred+116,210,Mathf.Min(540,parent.rect.height-24)));LayoutRebuilder.ForceRebuildLayoutImmediate(r);body.ForceMeshUpdate();PlaceImplicitLock();}
    void PlaceImplicitLock()
    {
        if(implicitLockIcon==null)return;
        bool show=item!=null&&!item.IsScrap&&item.ImplicitMod!=null;
        implicitLockIcon.gameObject.SetActive(show);
        if(!show)return;
        var info=body.textInfo;
        var chars=info.characterInfo;
        int header=-1;
        const string label="IMPLICIT";
        for(int i=0;i<=info.characterCount-label.Length;i++)
        {
            bool match=true;
            for(int j=0;j<label.Length;j++)if(chars[i+j].character!=label[j]){match=false;break;}
            if(match){header=i;break;}
        }
        if(header<0)return;
        int headerLine=chars[header].lineNumber;
        for(int i=header+label.Length;i<info.characterCount;i++)
        {
            if(chars[i].lineNumber<=headerLine||!chars[i].isVisible)continue;
            var glyph=chars[i];
            float x=glyph.bottomLeft.x-15f+body.rectTransform.rect.width*body.rectTransform.pivot.x;
            float y=(glyph.bottomLeft.y+glyph.topRight.y)*.5f;
            implicitLockIcon.rectTransform.anchoredPosition=new Vector2(x,y);
            return;
        }
    }
    public void LeaveSlot(ItemSlotUI slot) { if(owner==slot.transform) Hide(); }
    public void HideFor(RectTransform anchor) { if(owner==anchor) Hide(); }
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
            || (equipped && validatePlayerEquipment && EquipmentManager.Instance?.GetEquipped(item.ItemType)!=item)) { Hide(); return; }
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

/// <summary>Code-drawn UI padlock; no font fallback or additional item artwork.</summary>
public static class TooltipLockIcon
{
    static Sprite sprite;
    public static Sprite IconSprite
    {
        get
        {
            // Unity's destroyed-object null differs from C# reference null.
            if (sprite == null) sprite = Build();
            return sprite;
        }
    }
    static Sprite Build()
    {
        const int size=24;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false)
            {name="Permanent implicit padlock",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            bool body=x>=3&&x<=20&&y>=3&&y<=13;
            bool shackle=y>=12&&y<=20&&x>=6&&x<=17
                && (x<=8||x>=15||y>=18);
            if(!body&&!shackle)continue;
            bool rim=x<=4||x>=19||y<=4||y>=19;
            bool keyhole=body&&x>=11&&x<=12&&y>=7&&y<=10;
            pixels[y*size+x]=keyhole?new Color32(28,45,48,255)
                :rim?new Color32(45,74,75,255):new Color32(159,200,188,255);
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f);
    }
}
