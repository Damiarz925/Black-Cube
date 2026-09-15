// Developer map: Stable Resource paths for the supplied inventory composition and six ordinary currency icons.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class InventoryArtCatalog
{
    static readonly Dictionary<string, Sprite> Cache = new();
    public static Sprite Layout => Load("UI/Inventory/InventoryLayout");
    public static Sprite Currency(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => Load("UI/Currency/whiteToBlue"),
        CraftingCurrencyType.RerollMagic => Load("UI/Currency/RerollBlue"),
        CraftingCurrencyType.MagicToRare => Load("UI/Currency/BlueToYellow"),
        CraftingCurrencyType.RerollRareModifier => Load("UI/Currency/RerollYellow"),
        CraftingCurrencyType.AddRareModifier => Load("UI/Currency/AddYellow"),
        CraftingCurrencyType.RemoveRareModifier => Load("UI/Currency/Remove"),
        CraftingCurrencyType.AncientNormalToMagic => Load("UI/Currency/AncientWhiteToBlue"),
        CraftingCurrencyType.AncientMagicToRare => Load("UI/Currency/AncientBlueToYellow"),
        CraftingCurrencyType.AncientRareToLegendary => Load("UI/Currency/AncientYellowReroll"),
        CraftingCurrencyType.AncientReroll => Load("UI/Currency/AncientBlueReroll"),
        CraftingCurrencyType.AncientAddModifier => Load("UI/Currency/AncientAddYellow"),
        CraftingCurrencyType.AncientRemoveModifier => Load("UI/Currency/AncientRemove"),
        _ => null
    };

    public static string ResourcePath(CraftingCurrencyType type) => type switch
    {
        CraftingCurrencyType.NormalToMagic => "UI/Currency/whiteToBlue",
        CraftingCurrencyType.RerollMagic => "UI/Currency/RerollBlue",
        CraftingCurrencyType.MagicToRare => "UI/Currency/BlueToYellow",
        CraftingCurrencyType.RerollRareModifier => "UI/Currency/RerollYellow",
        CraftingCurrencyType.AddRareModifier => "UI/Currency/AddYellow",
        CraftingCurrencyType.RemoveRareModifier => "UI/Currency/Remove",
        CraftingCurrencyType.AncientNormalToMagic => "UI/Currency/AncientWhiteToBlue",
        CraftingCurrencyType.AncientMagicToRare => "UI/Currency/AncientBlueToYellow",
        CraftingCurrencyType.AncientRareToLegendary => "UI/Currency/AncientYellowReroll",
        CraftingCurrencyType.AncientReroll => "UI/Currency/AncientBlueReroll",
        CraftingCurrencyType.AncientAddModifier => "UI/Currency/AncientAddYellow",
        CraftingCurrencyType.AncientRemoveModifier => "UI/Currency/AncientRemove",
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
public sealed class InventoryArtworkHotspot:MonoBehaviour{}

/// <summary>Pixel-measured rectangles from InventoryLayout.png (960x1052, top-left source origin).</summary>
public static class InventoryArtLayout
{
    public const float Width=960f,Height=1052f;
    public static readonly Rect[] EquipmentSlots={P(72,160,153,237),P(176,160,257,237),P(282,160,363,237),P(388,160,469,237),P(494,160,575,237),P(599,160,681,237),P(705,160,787,237),P(811,160,893,237)};
    public static readonly Rect InventoryBounds=P(72,302,893,477);
    public static readonly Rect[] RelicSlots={P(164,529,263,615),P(336,529,438,615),P(520,529,623,615),P(697,529,799,615)};
    public static readonly Rect[] OrdinaryCurrencySlots={P(67,683,174,791),P(211,683,319,791),P(355,683,462,791),P(499,683,607,791),P(643,683,751,791),P(787,683,895,791)};
    public static readonly Rect[] AncientCurrencySlots={P(67,838,174,945),P(211,838,319,945),P(355,838,462,945),P(499,838,607,945),P(643,838,751,945),P(787,838,895,945)};
    public static readonly Rect[] OrdinaryCountBoxes={P(131,768,171,789),P(275,768,316,789),P(419,768,459,789),P(563,768,604,789),P(707,768,747,789),P(851,768,892,789)};
    public static readonly Rect[] AncientCountBoxes={P(131,922,171,944),P(275,922,316,944),P(419,922,459,944),P(563,922,604,944),P(707,922,747,944),P(851,922,892,944)};
    public static readonly Rect DismantleButton=P(568,88,724,130);
    public static readonly Rect HighlightButton=P(735,88,891,130);
    public static readonly Rect CloseButton=P(833,37,910,86);

    public static Rect P(float left,float top,float right,float bottom)=>new Rect(left/Width,1f-bottom/Height,(right-left)/Width,(bottom-top)/Height);
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

public sealed class BakedInventoryButtonFeedback:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
{
    bool hover,pressed,selected,engaged;Image image;
    void Awake(){image=GetComponent<Image>();Refresh();}
    public void SetEngaged(bool value){engaged=value;Refresh();}
    public void OnPointerEnter(PointerEventData _){hover=true;Refresh();}public void OnPointerExit(PointerEventData _){hover=false;pressed=false;Refresh();}
    public void OnPointerDown(PointerEventData _){pressed=true;Refresh();}public void OnPointerUp(PointerEventData _){pressed=false;Refresh();}
    public void OnSelect(BaseEventData _){selected=true;Refresh();}public void OnDeselect(BaseEventData _){selected=false;Refresh();}
    void Refresh(){if(image==null)image=GetComponent<Image>();if(image!=null)image.color=new Color(.75f,.82f,.9f,pressed ? .07f : (hover||selected||engaged ? .035f : .001f));}
}
