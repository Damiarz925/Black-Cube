// Pause-menu reference browser. All families and tier data are read from the
// same ModDatabase, canonical slot pools and PoedbAffixCatalog used by rolling.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CodexSideFilter { All, Prefixes, Suffixes }

public sealed class CodexModFamily
{
    public StatTypes Stat { get; }
    public AffixDefinitions Definition { get; }
    public IReadOnlyList<AffixTier> Tiers { get; }
    public CodexModFamily(StatTypes stat, AffixDefinitions definition, IReadOnlyList<AffixTier> tiers)
    { Stat = stat; Definition = definition; Tiers = tiers; }
}

public static class CodexModCatalog
{
    public static IReadOnlyList<CodexModFamily> Families(ModDatabase database,
        LootManager.GearType slot, CodexSideFilter filter)
    {
        if (database == null) return Array.Empty<CodexModFamily>();
        var result = new List<CodexModFamily>();
        var pool = GearStatLists.Instance != null
            ? GearStatLists.Instance.GetStatPoolForType(slot)
            : GearStatLists.GetCanonicalStatPoolForType(slot);
        foreach (StatTypes stat in pool.Distinct())
        {
            if (Gear.IsWeaponBaseStat(stat)) continue;
            var definition = database.GetDefinition(stat);
            if (definition == null) continue;
            if (filter == CodexSideFilter.Prefixes && definition.side != AffixSide.Prefix
                || filter == CodexSideFilter.Suffixes && definition.side != AffixSide.Suffix) continue;
            var tiers = ModManager.ApplicableTiers(definition, slot);
            if (tiers.Count == 0) continue;
            result.Add(new CodexModFamily(stat, definition,
                tiers.OrderBy(t => t.tierIndex).ToArray()));
        }
        return result.OrderBy(f => CategoryOrder(f.Stat))
            .ThenBy(f => Name(f), StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string Name(CodexModFamily family) =>
        string.IsNullOrWhiteSpace(family.Definition.displayName)
            ? StatDisplayFormatting.ToFriendlyName(family.Stat) : family.Definition.displayName;

    public static string Category(StatTypes stat) => StatCategoryMapping.GetCategory(stat) switch
    {
        StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage
            or StatCategory.Penetration => "DAMAGE",
        StatCategory.DamageOverTime or StatCategory.Ailments => "AILMENTS",
        StatCategory.Defenses => "DEFENSES / RESISTANCES",
        StatCategory.Resources => "LIFE / MANA",
        StatCategory.Attributes => "ATTRIBUTES",
        StatCategory.Utility => "UTILITY / CRITICAL",
        _ => "OTHER"
    };

    static int CategoryOrder(StatTypes stat) => StatCategoryMapping.GetCategory(stat) switch
    {
        StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage
            or StatCategory.Penetration => 0,
        StatCategory.DamageOverTime or StatCategory.Ailments => 1,
        StatCategory.Defenses => 2,
        StatCategory.Resources => 3,
        StatCategory.Attributes => 4,
        StatCategory.Utility => 5,
        _ => 6
    };

    public static string Format(ModDatabase database, LootManager.GearType slot,
        CodexSideFilter filter)
    {
        var text = new StringBuilder();
        text.AppendLine("<color=#A9C3CA>Any modifier valid for this item type may also appear as its permanent Implicit modifier.</color>");
        text.AppendLine("<color=#8B9299>Prefix/Suffix filters show normal explicit classifications.</color>");
        if (slot == LootManager.GearType.Weapons)
            text.AppendLine("<color=#8B9299>Element-specific weapon families require a compatible weapon base element.</color>");
        string category = null;
        foreach (var family in Families(database, slot, filter))
        {
            string next = Category(family.Stat);
            if (next != category)
            {
                category = next;
                text.AppendLine();
                text.AppendLine($"<color=#C9A86D><b>{category}</b></color>");
            }
            text.AppendLine($"<b>{Name(family).ToUpperInvariant()}</b>  <color=#8B9299>{family.Definition.side}</color>");
            foreach (var tier in family.Tiers)
            {
                string range = tier.pairedDamage
                    ? $"Adds ({tier.minValue:0.##}–{tier.maxValue:0.##}) to ({tier.minHighValue:0.##}–{tier.maxHighValue:0.##})"
                    : $"{tier.minValue:0.##}–{tier.maxValue:0.##}{(StatsComponent.IsPercentStat(family.Stat) ? "%" : "")}";
                text.AppendLine($"    T{tier.tierIndex}  |  ilvl {tier.minItemLevel}  |  {range}");
            }
        }
        return text.ToString().TrimEnd();
    }
}

public sealed class CodexModListUI : MonoBehaviour
{
    GameObject codexPage, listPage;
    TMP_Text listText, selectedType;
    ScrollRect scroll;
    ModDatabase database;
    LootManager.GearType slot = LootManager.GearType.Weapons;
    CodexSideFilter filter;
    public bool IsCodexPageOpen => codexPage != null && codexPage.activeSelf;
    public bool IsModListOpen => listPage != null && listPage.activeSelf;
    public LootManager.GearType SelectedType => slot;
    public CodexSideFilter SelectedFilter => filter;
    public string VisibleList => listText != null ? listText.text : string.Empty;
    public ScrollRect Scroll => scroll;

    public void Initialize(Transform pauseRoot, Action returnToPause)
    {
        database = ModManager.Instance?.Database;
        codexPage = Page(pauseRoot, "Codex Page");
        Title(codexPage.transform, "CODEX", .87f, 34);
        Button(codexPage.transform, "MOD LIST", new Vector2(.3f,.45f), new Vector2(.7f,.57f), OpenModList);
        Button(codexPage.transform, "BACK TO PAUSE", new Vector2(.3f,.15f), new Vector2(.7f,.27f), returnToPause);

        listPage = Page(pauseRoot, "Mod List Page");
        Title(listPage.transform, "MOD LIST", .93f, 31);
        selectedType = Title(listPage.transform, "WEAPON", .87f, 17);
        var slots = new[] { LootManager.GearType.Weapons, LootManager.GearType.Helmets,
            LootManager.GearType.BodyArmours, LootManager.GearType.Gloves,
            LootManager.GearType.Boots, LootManager.GearType.Amulets,
            LootManager.GearType.Rings, LootManager.GearType.Belts };
        for (int i=0;i<slots.Length;i++)
        {
            int index=i; int row=i/4, column=i%4;
            float x0=.04f+column*.235f, y0=.765f-row*.065f;
            Button(listPage.transform, ItemSlotUI.DisplayType(slots[i]).ToUpperInvariant(),
                new Vector2(x0,y0), new Vector2(x0+.218f,y0+.055f), () => SelectType(slots[index]));
        }
        var filters=new[]{CodexSideFilter.All,CodexSideFilter.Prefixes,CodexSideFilter.Suffixes};
        for(int i=0;i<filters.Length;i++)
        {
            int index=i;
            Button(listPage.transform, filters[i].ToString().ToUpperInvariant(),
                new Vector2(.22f+i*.19f,.595f),new Vector2(.39f+i*.19f,.64f),
                () => SelectFilter(filters[index]));
        }
        BuildScroll(listPage.transform);
        Button(listPage.transform,"BACK TO CODEX",new Vector2(.34f,.018f),new Vector2(.66f,.072f),ReturnToCodex);
        codexPage.SetActive(false);
        listPage.SetActive(false);
    }

    public void OpenCodex()
    {
        if(codexPage==null)return;
        listPage.SetActive(false); codexPage.SetActive(true); Time.timeScale=0f;
    }
    public void OpenModList()
    {
        if(listPage==null)return;
        codexPage.SetActive(false); listPage.SetActive(true); Refresh(); Time.timeScale=0f;
    }
    public void ReturnToCodex()
    {
        if(codexPage==null)return;
        listPage.SetActive(false); codexPage.SetActive(true); Time.timeScale=0f;
    }
    public void Hide(){codexPage?.SetActive(false);listPage?.SetActive(false);}
    public void SelectType(LootManager.GearType value){slot=value;Refresh();}
    public void SelectFilter(CodexSideFilter value){filter=value;Refresh();}
    void Refresh()
    {
        database = ModManager.Instance?.Database ?? database;
        if(selectedType!=null)selectedType.text=ItemSlotUI.DisplayType(slot).ToUpperInvariant();
        if(listText!=null)listText.text=CodexModCatalog.Format(database,slot,filter);
        if(scroll!=null)scroll.verticalNormalizedPosition=1f;
    }

    static GameObject Page(Transform root,string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(root,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(.09f,.06f);
        rect.anchorMax=new Vector2(.91f,.94f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        go.GetComponent<Image>().color=new Color(.025f,.03f,.04f,.985f);
        return go;
    }
    static TMP_Text Title(Transform parent,string value,float y,int size)
    {
        var go=new GameObject(value+" Label",typeof(RectTransform),typeof(TextMeshProUGUI));
        go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
        rect.anchorMin=new Vector2(.05f,y);rect.anchorMax=new Vector2(.95f,Mathf.Min(1f,y+.055f));
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        var text=go.GetComponent<TextMeshProUGUI>();text.text=value;text.fontSize=size;
        text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
        return text;
    }
    static Button Button(Transform parent,string value,Vector2 min,Vector2 max,Action action)
    {
        var go=new GameObject(value+" Button",typeof(RectTransform),typeof(Image),typeof(Button));
        go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=go.GetComponent<Image>();image.color=new Color(.105f,.13f,.16f,1f);
        var button=go.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());
        var label=Title(go.transform,value,.08f,15);var labelRect=label.rectTransform;
        labelRect.anchorMin=new Vector2(.02f,.08f);labelRect.anchorMax=new Vector2(.98f,.92f);
        CorruptionUIButtonSkin.Ensure(button);
        return button;
    }
    void BuildScroll(Transform parent)
    {
        var view=new GameObject("Mod List Scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect));
        view.transform.SetParent(parent,false);var rect=(RectTransform)view.transform;
        rect.anchorMin=new Vector2(.055f,.09f);rect.anchorMax=new Vector2(.945f,.575f);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        view.GetComponent<Image>().color=new Color(.06f,.07f,.085f,.97f);
        var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D));
        viewport.transform.SetParent(view.transform,false);var viewportRect=(RectTransform)viewport.transform;
        viewportRect.anchorMin=Vector2.zero;viewportRect.anchorMax=Vector2.one;
        viewportRect.offsetMin=new Vector2(12,8);viewportRect.offsetMax=new Vector2(-12,-8);
        viewport.GetComponent<Image>().color=Color.clear;
        var content=new GameObject("Authoritative Mod Text",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform,false);var contentRect=(RectTransform)content.transform;
        contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=Vector2.one;
        contentRect.pivot=new Vector2(.5f,1);contentRect.offsetMin=contentRect.offsetMax=Vector2.zero;
        listText=content.GetComponent<TextMeshProUGUI>();listText.fontSize=13;
        listText.color=Color.white;listText.raycastTarget=false;
        listText.textWrappingMode=TextWrappingModes.Normal;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll=view.GetComponent<ScrollRect>();scroll.viewport=viewportRect;scroll.content=contentRect;
        scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=35;
    }
}
