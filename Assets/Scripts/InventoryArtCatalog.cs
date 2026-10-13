// Developer map: Stable Resource paths for the supplied inventory composition and six ordinary currency icons.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InventoryArtCatalog
{
    static readonly Dictionary<string, Sprite> Cache = new();
    public static Sprite Layout => Load("UI/Inventory/InventoryUpdated");
    public static Sprite CurrencyBorderNormal=>Load("UI/Inventory/UpdatedUnhoveredBox");
    public static Sprite CurrencyBorderHovered=>Load("UI/Inventory/UpdatedHoveredBox");
    public static Sprite CurrencyBorderSelected=>Load("UI/Inventory/UpdatedSelectedBox");
    public static Sprite Currency(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => Load("UI/Currency/whiteToBlue"),
        CraftingCurrencyType.RerollMagic => Load("UI/Currency/RerollBlue"),
        CraftingCurrencyType.AddMagicModifier => null,
        CraftingCurrencyType.MagicToRare => Load("UI/Currency/BlueToYellow"),
        CraftingCurrencyType.RerollRareModifier => Load("UI/Currency/RerollYellow"),
        CraftingCurrencyType.AddRareModifier => Load("UI/Currency/AddYellow"),
        CraftingCurrencyType.RemoveRareModifier => Load("UI/Currency/Remove"),
        CraftingCurrencyType.AncientNormalToMagic => Load("UI/Currency/crystalline_orbital_sigil"),
        CraftingCurrencyType.AncientRerollMagic => Load("UI/Currency/cosmic_crystal_vortex_emblem"),
        CraftingCurrencyType.AncientAddMagicModifier => Load("UI/Currency/celestial_crystal_cross_emblem"),
        CraftingCurrencyType.AncientMagicToRare => Load("UI/Currency/radiant_crystal_orbit_emblem"),
        CraftingCurrencyType.AncientRerollRareModifier => Load("UI/Currency/radiant_golden_spiral_emblem"),
        CraftingCurrencyType.AncientAddRareModifier => Load("UI/Currency/radiant_golden_crystal_plus_emblem"),
        CraftingCurrencyType.AncientRemoveModifier => Load("UI/Currency/shattered_crimson_cosmic_emblem"),
        CraftingCurrencyType.AncientPrefixFocus => Load("UI/Currency/crystalline_cosmic_ascension_emblem"),
        CraftingCurrencyType.AncientSuffixFocus => Load("UI/Currency/celestial_crystal_arrow_emblem"),
        CraftingCurrencyType.EmpowermentCatalyst => PlaceholderIcon.EmpowermentCatalyst,
        CraftingCurrencyType.AffixFocus => Resources.Load<Sprite>("UI/Currency/AffixFocus") ?? PlaceholderIcon.EmpowermentCatalyst,
        _ => null
    };

    public static string ResourcePath(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => "UI/Currency/whiteToBlue",
        CraftingCurrencyType.RerollMagic => "UI/Currency/RerollBlue",
        CraftingCurrencyType.AddMagicModifier => null,
        CraftingCurrencyType.MagicToRare => "UI/Currency/BlueToYellow",
        CraftingCurrencyType.RerollRareModifier => "UI/Currency/RerollYellow",
        CraftingCurrencyType.AddRareModifier => "UI/Currency/AddYellow",
        CraftingCurrencyType.RemoveRareModifier => "UI/Currency/Remove",
        CraftingCurrencyType.AncientNormalToMagic => "UI/Currency/crystalline_orbital_sigil",
        CraftingCurrencyType.AncientRerollMagic => "UI/Currency/cosmic_crystal_vortex_emblem",
        CraftingCurrencyType.AncientAddMagicModifier => "UI/Currency/celestial_crystal_cross_emblem",
        CraftingCurrencyType.AncientMagicToRare => "UI/Currency/radiant_crystal_orbit_emblem",
        CraftingCurrencyType.AncientRerollRareModifier => "UI/Currency/radiant_golden_spiral_emblem",
        CraftingCurrencyType.AncientAddRareModifier => "UI/Currency/radiant_golden_crystal_plus_emblem",
        CraftingCurrencyType.AncientRemoveModifier => "UI/Currency/shattered_crimson_cosmic_emblem",
        CraftingCurrencyType.AncientPrefixFocus => "UI/Currency/crystalline_cosmic_ascension_emblem",
        CraftingCurrencyType.AncientSuffixFocus => "UI/Currency/celestial_crystal_arrow_emblem",
        CraftingCurrencyType.AffixFocus => "UI/Currency/AffixFocus",
        _ => null
    };

    static Sprite Load(string path)
    {
        if(Cache.TryGetValue(path,out var cached))return cached;
        var sprite=Resources.Load<Sprite>(path);
        Cache[path]=sprite;
        if(sprite==null)Debug.LogWarning("InventoryArtCatalog: missing sprite "+path);
        return sprite;
    }
}

/// <summary>Small first-party placeholder icons for relic identity only.</summary>
public static class PlaceholderIcon
{
    static readonly Dictionary<string, Sprite> Icons = new();
    public static Sprite Relic(LootManager.GearRarity rarity,int cycle)
    {
        string key="relic/"+rarity+"/"+cycle;
        if(Icons.TryGetValue(key,out var existing)&&existing!=null)return existing;
        Color32 tint=rarity switch
        {
            LootManager.GearRarity.Magic=>new Color32(84,147,230,255),
            LootManager.GearRarity.Rare=>new Color32(224,178,67,255),
            LootManager.GearRarity.Legendary=>new Color32(227,108,69,255),
            _=>new Color32(190,196,212,255)
        };
        return Icons[key]=Build(key,tint,Mathf.Abs(cycle)%6);
    }
    public static Sprite EmpowermentCatalyst
    {
        get
        {
            const string key="currency/empowerment";
            if(Icons.TryGetValue(key,out var existing)&&existing!=null)return existing;
            return Icons[key]=Build(key,new Color32(52,185,222,255),4);
        }
    }
    static Sprite Build(string name,Color32 tint,int mark)
    {
        const int size=64;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            int dx=Mathf.Abs(x-31),dy=Mathf.Abs(y-31),diamond=dx+dy;
            if(diamond>29)continue;
            bool rim=diamond>=25;
            bool rune=mark switch
            {
                0=>dy<3&&dx<16||dx<3&&dy<16,
                1=>dy<3&&dx<17||dx>=12&&dx<=15&&dy<12,
                2=>dy<3&&dx<17||dx<3&&dy<17||dx==dy&&dx<13,
                3=>dx==dy&&dx<17||dx+dy<5,
                4=>dx<3&&dy<17||dy<3&&dx<17,
                _=>dx+dy>=13&&dx+dy<=17||dx+dy<5
            };
            pixels[y*size+x]=rim?new Color32(245,234,208,255):rune?new Color32(255,250,230,255):new Color32((byte)(tint.r/2),(byte)(tint.g/2),(byte)(tint.b/2),255);
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f);
    }
}

/// <summary>Marks an interactive overlay whose visuals come from InventoryLayout.png.</summary>
public sealed partial class InventoryArtworkHotspot:MonoBehaviour{}

/// <summary>Pixel-measured rectangles from InventoryLayout.png (960x1052, top-left source origin).</summary>
public static class InventoryArtLayout
{
    public const float Width=1192f,Height=1320f;
    public static readonly Rect[] EquipmentSlots={P(515,128,655,278),P(700,250,765,315),P(490,300,680,555),P(300,370,450,525),P(710,370,860,525),P(780,250,845,315),P(500,560,670,635),P(95,330,285,585),P(895,330,1085,585)};
    public static readonly Rect InventoryBounds=P(75,620,1125,1005);
    public static readonly Rect[] RelicSlots={P(164,529,263,615),P(336,529,438,615),P(520,529,623,615),P(697,529,799,615)};
    public static readonly Rect[] OrdinaryCurrencySlots=CurrencyRow();
    public static readonly Rect[] AncientCurrencySlots=CurrencyRow();
    public static readonly Rect[] OrdinaryCountBoxes=CountRow();
    public static readonly Rect[] AncientCountBoxes=CountRow();
    public static readonly Rect DismantleButton=P(568,88,724,130);
    public static readonly Rect HighlightButton=P(735,88,891,130);
    public static readonly Rect CloseButton=P(833,37,910,86);

    public static Rect P(float left,float top,float right,float bottom)=>new Rect(left/Width,1f-bottom/Height,(right-left)/Width,(bottom-top)/Height);
    static Rect[] CurrencyRow(){var result=new Rect[9];for(int i=0;i<9;i++){float left=43+i*126;result[i]=P(left,1172,left+111,1272);}return result;}
    static Rect[] CountRow(){var result=new Rect[9];for(int i=0;i<9;i++){float left=43+i*126;result[i]=P(left+72,1246,left+107,1270);}return result;}
    public static Rect CurrencyRowForEditor(int index)=>OrdinaryCurrencySlots[index];
    public static Rect CountRowForEditor(int index)=>OrdinaryCountBoxes[index];
    public static Rect Relative(Rect child,Rect parent)=>new Rect((child.xMin-parent.xMin)/parent.width,(child.yMin-parent.yMin)/parent.height,child.width/parent.width,child.height/parent.height);
    public static Rect Inset(Rect rect,float horizontalPixels,float verticalPixels)=>new Rect(rect.x+horizontalPixels/Width,rect.y+verticalPixels/Height,rect.width-2f*horizontalPixels/Width,rect.height-2f*verticalPixels/Height);
    public static void Apply(RectTransform target,Rect rect){target.anchorMin=rect.min;target.anchorMax=rect.max;target.offsetMin=target.offsetMax=Vector2.zero;}
}

public static class BakedInventoryButton
{
    public static void Configure(Button button,Rect rect)
    {
        if(button==null)return;InventoryArtLayout.Apply((RectTransform)button.transform,rect);
        if(button.GetComponent<InventoryArtworkHotspot>()==null)button.gameObject.AddComponent<InventoryArtworkHotspot>();
        var image=button.GetComponent<Image>();if(image==null)image=button.gameObject.AddComponent<Image>();image.sprite=null;image.color=new Color(1,1,1,.001f);button.targetGraphic=image;button.transition=Selectable.Transition.None;
        foreach(var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true))text.gameObject.SetActive(false);
        var skin=button.GetComponent<CorruptionUIButtonSkin>();if(skin!=null)skin.enabled=false;
        foreach(var child in button.GetComponentsInChildren<Transform>(true))if(child!=button.transform&&(child.name=="Frame"||child.name=="Corruption art"))child.gameObject.SetActive(false);
        if(button.GetComponent<BakedInventoryButtonFeedback>()==null)button.gameObject.AddComponent<BakedInventoryButtonFeedback>();
    }
    public static void SetEngaged(Button button,bool value){button?.GetComponent<BakedInventoryButtonFeedback>()?.SetEngaged(value);}
}

public sealed partial class BakedInventoryButtonFeedback:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
{
    bool hover,pressed,selected,engaged;Image image;
    void Awake(){image=GetComponent<Image>();Refresh();}
    public void SetEngaged(bool value){engaged=value;Refresh();}
    public void OnPointerEnter(PointerEventData _){hover=true;Refresh();}public void OnPointerExit(PointerEventData _){hover=false;pressed=false;Refresh();}
    public void OnPointerDown(PointerEventData _){pressed=true;Refresh();}public void OnPointerUp(PointerEventData _){pressed=false;Refresh();}
    public void OnSelect(BaseEventData _){selected=true;Refresh();}public void OnDeselect(BaseEventData _){selected=false;Refresh();}
    void Refresh(){if(image==null)image=GetComponent<Image>();if(image!=null)image.color=new Color(.75f,.82f,.9f,pressed ? .07f : (hover||selected||engaged ? .035f : .001f));}
}
