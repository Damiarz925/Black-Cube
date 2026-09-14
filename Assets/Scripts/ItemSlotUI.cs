// Developer map: Binds a Gear to its grid button, rarity visuals and pointer actions. Left click equips, right click requests dismantling; Inventory remains the authority for allowed actions.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]   //References to UI elements owned by this UI slot
    [SerializeField] private TextMeshProUGUI nameText;  //Reference for the Name of the Item
    [SerializeField] private TextMeshProUGUI detailText;    //Reference for the item's details
    [SerializeField] private Button button; //Reference for the item button (to equip)
    [SerializeField] private Image iconImage;   //Image to be used for the item.
    [SerializeField] private Image backgroundImage;     //Image for the item background
    [SerializeField] private Image rarityOverlay;       //Image used for the rarity overlay

    private Gear gear;
    public Gear Item => gear;
    public Image IconImage => iconImage;
    public Image BackgroundImage => slotInterior != null ? slotInterior : backgroundImage;
    public Image RarityOverlay => rarityOverlay;
    public Image ElementBadge => elementBadge;
    private Image elementBadge;
    private Image slotInterior;
    private GameObject areaCorruptionBorder;
    private GameObject modHighlight;
    public void OnPointerClick(PointerEventData data)
    {
        if (CurrencyInventory.Instance != null && CurrencyInventory.Instance.ArmedCurrency.HasValue && data.button == PointerEventData.InputButton.Right)
        {
            CurrencyInventory.Instance.CancelArmed();
            return;
        }
        if (data.button == PointerEventData.InputButton.Right && gear != null)
            Inventory.Instance?.TryDismantle(gear);
    }
    public void OnPointerEnter(PointerEventData data) => GetComponentInParent<InventoryUI>()?.ShowTooltip(gear, (RectTransform)transform, false, data);
    public void OnPointerExit(PointerEventData data) => GetComponentInParent<InventoryUI>()?.Tooltip?.Leave((RectTransform)transform, data);

    public void Setup(Gear gear)    //Calls setup with just gear and no sprite
    {
        Setup(gear, null);
    }

    public void Setup(Gear gear, Sprite icon) //Populates the item slot UI with gear data and click behavior
    {
        this.gear = gear; //Sets the private gear object of the class to the passed in gear object
        var glyph = GetComponentInChildren<EquipmentGlyph>(true);
        if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();
        if (backgroundImage != null)
        {
            if(backgroundImage.transform==transform)
            {
                backgroundImage.color=new Color(1,1,1,.001f);
                if(slotInterior==null){var interior=new GameObject("Rarity slot interior",typeof(RectTransform),typeof(Image));interior.transform.SetParent(transform,false);Place((RectTransform)interior.transform,.1f,.12f,.9f,.9f);slotInterior=interior.GetComponent<Image>();slotInterior.raycastTarget=false;slotInterior.transform.SetAsFirstSibling();}
            }
            else slotInterior=backgroundImage;
            slotInterior.color=RarityBackground(gear.ItemRarity);
        }
        if (rarityOverlay != null) rarityOverlay.gameObject.SetActive(false);
        if (glyph != null) glyph.enabled = false;

        if (icon != null && iconImage == null)
        {
            var iconObject = new GameObject("Themed item icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
        }

        if (nameText != null)   //If there is name text, set it to be the rarity + item type
            nameText.gameObject.SetActive(false);

        if (detailText != null) //If there is detail text, set it to be "ilvl" + the gear's item level
        {
            detailText.text = gear.IsScrap ? $"x{gear.StackCount}" : $"LV {gear.ItemLevel}";
            detailText.fontSize = 12; detailText.alignment = TextAlignmentOptions.Center;
            Place(detailText.rectTransform, .05f, .02f, .78f, .2f);
        }
        if (backgroundImage != null && backgroundImage.transform != transform) Place(backgroundImage.rectTransform, .1f, .12f, .9f, .9f);
        if (iconImage != null) Place(iconImage.rectTransform, .13f, .19f, .87f, .89f);

        if(elementBadge == null)
        {
            var go = new GameObject("Base element marker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            elementBadge = go.GetComponent<Image>(); elementBadge.raycastTarget = false;
        }
        var marker = elementBadge.rectTransform;
        marker.anchorMin = marker.anchorMax = gear.IsScrap ? new Vector2(.5f,.6f) : new Vector2(.82f,.18f);
        marker.anchoredPosition = Vector2.zero; marker.sizeDelta = gear.IsScrap ? new Vector2(20,15) : new Vector2(4,4);
        marker.localRotation = Quaternion.Euler(0,0,gear.IsScrap ? 15 : 45);
        elementBadge.enabled = gear.IsScrap || gear.ItemType == LootManager.GearType.Weapons;
        elementBadge.color = gear.IsScrap ? new Color(.7f,.72f,.75f) : ItemTooltipUI.ElementTint(gear.BaseElement);
        if(glyph != null && gear.IsScrap) glyph.enabled = false;

        if (iconImage != null)  //If the icon image exists, set iconImage.sprite to the passed icon, then enable
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
            iconImage.color = Color.white;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
        }

        if (button != null)     //If the button exists, remove all listeners and assign the OnClick method from this class (prevents one click having duplicate actions)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    static void Place(RectTransform r, float x0, float y0, float x1, float y1)
    { r.anchorMin = new Vector2(x0,y0); r.anchorMax = new Vector2(x1,y1); r.offsetMin = r.offsetMax = Vector2.zero; }

    public void SetModHighlight(bool highlighted, int _)
    {
        if (modHighlight == null)
        {
            modHighlight = new GameObject("Mod filter highlight border", typeof(RectTransform), typeof(Image));
            modHighlight.transform.SetParent(transform, false);
            Place((RectTransform)modHighlight.transform, .1f, .12f, .9f, .9f);
            Image border = modHighlight.GetComponent<Image>();
            border.sprite = ItemIconCatalog.HighlightBorder;
            border.color = Color.white;
            border.type = Image.Type.Simple;
            border.preserveAspect = true;
            border.raycastTarget = false;
        }

        Image highlightImage = modHighlight.GetComponent<Image>();
        if (highlightImage.sprite == null) highlightImage.sprite = ItemIconCatalog.HighlightBorder;
        modHighlight.transform.SetAsLastSibling();
        modHighlight.SetActive(highlighted);
    }

    public void SetAreaCorruption(int stage)
    {
        if (areaCorruptionBorder == null)
        {
            areaCorruptionBorder = new GameObject("Area corruption border", typeof(RectTransform), typeof(Image));
            areaCorruptionBorder.transform.SetParent(transform, false);
            Place((RectTransform)areaCorruptionBorder.transform, .1f, .12f, .9f, .9f);
            Image border = areaCorruptionBorder.GetComponent<Image>();
            border.color = Color.white;
            border.type = Image.Type.Simple;
            border.preserveAspect = true;
            border.raycastTarget = false;
        }

        Image borderImage = areaCorruptionBorder.GetComponent<Image>();
        borderImage.sprite = ItemIconCatalog.GetCorruptionBorder(stage);
        areaCorruptionBorder.SetActive(gear != null && !gear.IsScrap && borderImage.sprite != null);
        areaCorruptionBorder.transform.SetAsLastSibling();
        if (modHighlight != null) modHighlight.transform.SetAsLastSibling();
    }

    private void OnClick()  //When the button is clicked, call the Equip function from EquipmentManager, passing in the private gear object from this class. (Assumes equipment manager is initialized already)
    {
        if (CurrencyInventory.Instance != null && CurrencyInventory.Instance.ArmedCurrency.HasValue)
        {
            if (gear == null || gear.IsScrap) CurrencyInventory.Instance.CancelArmed();
            else CurrencyInventory.Instance.TryApplyArmedToGear(gear,CraftingCurrencyInput.ShiftHeld);
            return;
        }
        if (gear == null || gear.IsScrap) return;
        EquipmentManager.Instance?.Equip(gear);
    }

    public static string DisplayType(LootManager.GearType type) => type switch
    {
        LootManager.GearType.Helmets => "Helmet",
        LootManager.GearType.Amulets => "Amulet",
        LootManager.GearType.BodyArmours => "Body Armor",
        LootManager.GearType.Gloves => "Glove",
        LootManager.GearType.Boots => "Boot",
        LootManager.GearType.Rings => "Ring",
        LootManager.GearType.Belts => "Belt",
        _ => "Weapon"
    };

    public static Color RarityColor(LootManager.GearRarity rarity)
    {
        return rarity switch
        {
            LootManager.GearRarity.Normal => new Color(1f, 1f, 1f, 1f),
            LootManager.GearRarity.Magic => new Color(0.3f, 0.5f, 1f, 1f),
            LootManager.GearRarity.Rare => new Color(1f, 0.85f, 0.2f, 1f),
            LootManager.GearRarity.Legendary => new Color(1f, 0.6f, 0.15f, 1f),
            _ => new Color(1f, 1f, 1f, 0.25f)
        };
    }

    public static Color RarityBackground(LootManager.GearRarity rarity)
    {
        return rarity switch
        {
            LootManager.GearRarity.Magic => new Color(.068f, .082f, .115f, 1f),
            LootManager.GearRarity.Rare => new Color(.105f, .096f, .057f, 1f),
            LootManager.GearRarity.Legendary => new Color(.115f, .072f, .044f, 1f),
            _ => new Color(.055f, .058f, .067f, 1f)
        };
    }
}
