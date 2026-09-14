// Developer map: Exact pixel map and stable runtime views for the supplied 2172x724 top-HUD artwork.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TopHUDButtonKind { Skills, Passives, Enemy, Inventory, Stats, Pause, Play }

public static class TopHUDLayout
{
    public const int SourceWidth = 2172;
    public const int SourceHeight = 724;
    // The source PNG contains an opaque black export canvas. This is the measured artwork rectangle.
    public static readonly RectInt Artwork = new RectInt(12, 169, 2148, 232);
    public static readonly RectInt PlayerPortrait = new RectInt(51, 191, 132, 164);
    public static readonly RectInt PlayerName = new RectInt(214, 214, 427, 38);
    public static readonly RectInt PlayerHealth = new RectInt(260, 275, 394, 26);
    // The player HP artwork has a wider heart cap than the other resource rails.
    public static readonly RectOffset PlayerHealthFillInset = new RectOffset(4, 4, 2, 2);
    public static readonly RectInt PlayerMana = new RectInt(260, 325, 394, 26);
    public static readonly RectInt EnemyPortrait = new RectInt(726, 191, 132, 164);
    public static readonly RectInt EnemyName = new RectInt(930, 214, 420, 38);
    public static readonly RectInt EnemyHealth = new RectInt(980, 275, 346, 26);
    public static readonly RectInt EnemyMana = new RectInt(970, 325, 356, 26);
    // Unequal bay interiors measured from the completed guide's vertical frame edges.
    public static readonly RectInt Skills = new RectInt(1418, 216, 123, 143);
    public static readonly RectInt Passives = new RectInt(1543, 216, 118, 143);
    public static readonly RectInt Enemy = new RectInt(1663, 216, 122, 143);
    public static readonly RectInt Inventory = new RectInt(1787, 216, 118, 143);
    public static readonly RectInt Stats = new RectInt(1907, 216, 112, 143);
    public static readonly RectInt Pause = new RectInt(2049, 198, 65, 79);
    public static readonly RectInt Play = new RectInt(2049, 291, 65, 79);

    public static readonly RectInt[] MappedRects =
    {
        PlayerPortrait, PlayerName, PlayerHealth, PlayerMana, EnemyPortrait, EnemyName,
        EnemyHealth, EnemyMana, Skills, Passives, Enemy, Inventory, Stats, Pause, Play
    };

    public static float Aspect => Artwork.width / (float)Artwork.height;

    public static Rect Normalized(RectInt sourceTopLeft)
    {
        float x = (sourceTopLeft.x - Artwork.x) / (float)Artwork.width;
        float width = sourceTopLeft.width / (float)Artwork.width;
        float yMin = 1f - (sourceTopLeft.yMax - Artwork.y) / (float)Artwork.height;
        float height = sourceTopLeft.height / (float)Artwork.height;
        return new Rect(x, yMin, width, height);
    }

    public static bool IsContained(RectInt rect) => rect.xMin >= Artwork.xMin && rect.xMax <= Artwork.xMax
        && rect.yMin >= Artwork.yMin && rect.yMax <= Artwork.yMax;

    public static RectInt ButtonRect(TopHUDButtonKind kind) => kind switch
    {
        TopHUDButtonKind.Skills => Skills,
        TopHUDButtonKind.Passives => Passives,
        TopHUDButtonKind.Enemy => Enemy,
        TopHUDButtonKind.Inventory => Inventory,
        TopHUDButtonKind.Stats => Stats,
        TopHUDButtonKind.Pause => Pause,
        _ => Play
    };

    // Measured non-black state artwork bounds. Coordinates are top-left source pixels.
    public static RectInt StateSlice(TopHUDButtonKind kind, int state) => (kind, Mathf.Clamp(state, 0, 2)) switch
    {
        (TopHUDButtonKind.Skills, 0) => new RectInt(288,150,391,389),
        (TopHUDButtonKind.Skills, 1) => new RectInt(891,151,390,390),
        (TopHUDButtonKind.Skills, _) => new RectInt(1492,151,391,389),
        (TopHUDButtonKind.Passives, 0) => new RectInt(352,164,395,380),
        (TopHUDButtonKind.Passives, 1) => new RectInt(880,157,410,394),
        (TopHUDButtonKind.Passives, _) => new RectInt(1419,157,407,394),
        (TopHUDButtonKind.Enemy, 0) => new RectInt(296,154,426,414),
        (TopHUDButtonKind.Enemy, 1) => new RectInt(873,154,426,414),
        (TopHUDButtonKind.Enemy, _) => new RectInt(1450,154,426,414),
        (TopHUDButtonKind.Inventory, 0) => new RectInt(423,171,374,351),
        (TopHUDButtonKind.Inventory, 1) => new RectInt(899,160,374,374),
        (TopHUDButtonKind.Inventory, _) => new RectInt(1375,168,374,359),
        (TopHUDButtonKind.Stats, 0) => new RectInt(363,161,376,359),
        (TopHUDButtonKind.Stats, 1) => new RectInt(898,154,376,366),
        (TopHUDButtonKind.Stats, _) => new RectInt(1433,157,376,363),
        (TopHUDButtonKind.Pause, 0) => new RectInt(428,155,374,379),
        (TopHUDButtonKind.Pause, 1) => new RectInt(899,151,372,385),
        (TopHUDButtonKind.Pause, _) => new RectInt(1368,153,373,382),
        (TopHUDButtonKind.Play, 0) => new RectInt(370,182,349,343),
        (TopHUDButtonKind.Play, 1) => new RectInt(901,173,374,356),
        _ => new RectInt(1454,182,349,343)
    };

    public static string ResourceName(TopHUDButtonKind kind) => "UI/TopHUD/" + kind + "Button";

    public static void Apply(RectTransform target, RectInt sourceTopLeft)
    {
        Rect normalized = Normalized(sourceTopLeft);
        target.anchorMin = normalized.min;
        target.anchorMax = normalized.max;
        target.offsetMin = target.offsetMax = Vector2.zero;
        target.localScale = Vector3.one;
    }
}

public sealed class HUDSpriteState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Button button;
    Image image;
    Sprite normal;
    Sprite hover;
    Sprite pressed;
    bool pointerOver;
    bool pointerDown;
    bool focused;
    bool persistentActive;

    public bool PersistentActive => persistentActive;
    public int VisualStateIndex => button != null && !button.IsInteractable() ? -1
        : persistentActive ? 2 : pointerDown ? 2 : pointerOver || focused ? 1 : 0;

    public void Initialize(Button owner, TopHUDButtonKind kind)
    {
        button = owner;
        image = owner.targetGraphic as Image;
        if (image == null) image = owner.GetComponent<Image>();
        Texture2D sheet = Resources.Load<Texture2D>(TopHUDLayout.ResourceName(kind));
        normal = Create(sheet, TopHUDLayout.StateSlice(kind, 0), kind + " Normal");
        hover = Create(sheet, TopHUDLayout.StateSlice(kind, 1), kind + " Hover");
        pressed = Create(sheet, TopHUDLayout.StateSlice(kind, 2), kind + " Pressed");
        owner.transition = Selectable.Transition.None;
        owner.targetGraphic = image;
        image.preserveAspect = false;
        image.raycastTarget = true;
        Refresh();
    }

    public void SetPersistentActive(bool value)
    {
        if (persistentActive == value) return;
        persistentActive = value;
        Refresh();
    }

    public void Refresh()
    {
        if (image == null) return;
        int state = VisualStateIndex;
        image.sprite = state == 2 ? pressed : state == 1 ? hover : normal;
        image.color = state < 0 ? new Color(.42f,.42f,.42f,1f) : Color.white;
        image.enabled = image.sprite != null;
    }

    public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { pointerOver = false; pointerDown = false; Refresh(); }
    public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pointerDown = true; Refresh(); }
    public void OnPointerUp(PointerEventData eventData) { pointerDown = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { focused = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { focused = false; pointerDown = false; Refresh(); }
    void OnDisable() { pointerOver = pointerDown = focused = false; Refresh(); }

    static Sprite Create(Texture2D texture, RectInt topLeft, string spriteName)
    {
        if (texture == null) return null;
        Rect rect = new Rect(topLeft.x, texture.height - topLeft.yMax, topLeft.width, topLeft.height);
        Sprite sprite = Sprite.Create(texture, rect, Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        return sprite;
    }
}

public sealed class HUDResourceBar : MaskableGraphic
{
    [SerializeField, Range(0f,1f)] float fillAmount;
    public float FillAmount { get => fillAmount; set { value = Mathf.Clamp01(value); if (Mathf.Approximately(fillAmount,value)) return; fillAmount=value; SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r=GetPixelAdjustedRect();r.width*=fillAmount;
        if(r.width<=0f||r.height<=0f)return;
        UIVertex v=UIVertex.simpleVert;v.color=color;
        v.position=new Vector2(r.xMin,r.yMin);vh.AddVert(v);v.position=new Vector2(r.xMin,r.yMax);vh.AddVert(v);
        v.position=new Vector2(r.xMax,r.yMax);vh.AddVert(v);v.position=new Vector2(r.xMax,r.yMin);vh.AddVert(v);
        vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
    }
}

public sealed class HUDPortraitMaskGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=GetPixelAdjustedRect();float cut=Mathf.Min(r.width*.18f,r.height*.14f);
        Vector2[] edge={new(r.xMin+cut,r.yMin),new(r.xMax-cut,r.yMin),new(r.xMax,r.yMin+cut),new(r.xMax,r.yMax-cut),new(r.xMax-cut,r.yMax),new(r.xMin+cut,r.yMax),new(r.xMin,r.yMax-cut),new(r.xMin,r.yMin+cut)};
        UIVertex v=UIVertex.simpleVert;v.color=Color.white;v.position=r.center;vh.AddVert(v);foreach(Vector2 point in edge){v.position=point;vh.AddVert(v);}for(int i=0;i<8;i++)vh.AddTriangle(0,i+1,(i+1)%8+1);
    }
}

public readonly struct HUDPortraitFraming
{
    public readonly float Scale;
    public readonly Vector2 Offset;
    public HUDPortraitFraming(float scale,Vector2 offset){Scale=scale;Offset=offset;}

    public static HUDPortraitFraming Calculate(Sprite sprite,bool enemy)
    {
        float minimum=enemy?2.6f:1.5f;
        if(sprite==null)return new HUDPortraitFraming(1f,Vector2.zero);
        Vector2[] vertices=sprite.vertices;
        if(vertices!=null&&vertices.Length>=3)
        {
            Vector2 min=vertices[0],max=vertices[0];
            for(int i=1;i<vertices.Length;i++){min=Vector2.Min(min,vertices[i]);max=Vector2.Max(max,vertices[i]);}
            float ppu=Mathf.Max(.01f,sprite.pixelsPerUnit);Vector2 full=sprite.rect.size/ppu;
            float occupied=Mathf.Max((max.x-min.x)/full.x,(max.y-min.y)/full.y);
            if(occupied<.985f)
            {
                float scale=Mathf.Clamp(.88f/Mathf.Max(.01f,occupied),minimum,enemy?4.5f:3.25f);
                Vector2 centerPixels=(min+max)*.5f*ppu+sprite.pivot;
                Vector2 center=new(centerPixels.x/sprite.rect.width,centerPixels.y/sprite.rect.height);
                return new HUDPortraitFraming(scale,new Vector2(.5f-center.x,.5f-center.y));
            }
        }
        try
        {
            Rect r=sprite.textureRect;Color32[] pixels=sprite.texture.GetPixels32();int width=sprite.texture.width;
            int x0=Mathf.FloorToInt(r.x),y0=Mathf.FloorToInt(r.y),rw=Mathf.RoundToInt(r.width),rh=Mathf.RoundToInt(r.height);
            int minX=rw,minY=rh,maxX=-1,maxY=-1;
            for(int y=0;y<rh;y++)for(int x=0;x<rw;x++)if(pixels[(y0+y)*width+x0+x].a>12){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            if(maxX>=minX&&maxY>=minY)
            {
                float occupied=Mathf.Max((maxX-minX+1f)/rw,(maxY-minY+1f)/rh);
                float scale=Mathf.Clamp(.88f/Mathf.Max(.01f,occupied),minimum,enemy?4.5f:3.25f);
                Vector2 center=new((minX+maxX+1f)/(2f*rw),(minY+maxY+1f)/(2f*rh));
                return new HUDPortraitFraming(scale,new Vector2(.5f-center.x,.5f-center.y));
            }
        }
        // Imported sprites are intentionally non-readable in the authored battle prefabs.
        // Depending on Unity version GetPixels32 throws either UnityException or ArgumentException.
        catch(Exception) { }
        return new HUDPortraitFraming(minimum,enemy?new Vector2(.065f,.07f):new Vector2(0f,-.02f));
    }

    public void Apply(RectTransform rect)
    {
        Vector2 size=Vector2.one*Scale,center=Vector2.one*.5f+Offset*Scale;
        rect.anchorMin=center-size*.5f;rect.anchorMax=center+size*.5f;rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
}

public sealed class PlayerDisplayNameProvider : MonoBehaviour
{
    const string PreferenceKey="BlackCube.PlayerDisplayName";
    [SerializeField] string displayName="Wanderer";
    public string DisplayName => Normalize(displayName);
    public event Action Changed;
    void Awake() { displayName=Normalize(PlayerPrefs.GetString(PreferenceKey,DisplayName)); }
    public static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? "Wanderer" : value.Trim();
    public void SetDisplayName(string value)
    {
        string next=Normalize(value);
        if(next==DisplayName)return;
        displayName=next;PlayerPrefs.SetString(PreferenceKey,displayName);PlayerPrefs.Save();Changed?.Invoke();
    }
}
