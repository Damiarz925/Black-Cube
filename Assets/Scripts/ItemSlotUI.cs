using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI References")]   //References to UI elements owned by this UI slot
    [SerializeField] private TextMeshProUGUI nameText;  //Reference for the Name of the Item
    [SerializeField] private TextMeshProUGUI detailText;    //Reference for the item's details
    [SerializeField] private Button button; //Reference for the item button (to equip)
    [SerializeField] private Image iconImage;   //Image to be used for the item.
    [SerializeField] private Image backgroundImage;     //Image for the item background
    [SerializeField] private Image rarityOverlay;       //Image used for the rarity overlay

    private Gear gear;

    public void Setup(Gear gear)    //Calls setup with just gear and no sprite
    {
        Setup(gear, null);
    }

    public void Setup(Gear gear, Sprite icon) //Populates the item slot UI with gear data and click behavior
    {
        this.gear = gear; //Sets the private gear object of the class to the passed in gear object
        backgroundImage.color = RarityColor(gear.ItemRarity);

        if (nameText != null)   //If there is name text, set it to be the rarity + item type
            nameText.text = $"{gear.ItemRarity} {gear.ItemType}";

        if (detailText != null) //If there is detail text, set it to be "ilvl" + the gear's item level
            detailText.text = $"ilvl {gear.ItemLevel}";

        if (iconImage != null)  //If the icon image exists, set iconImage.sprite to the passed icon, then enable
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        if (button != null)     //If the button exists, remove all listeners and assign the OnClick method from this class (prevents one click having duplicate actions)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()  //When the button is clicked, call the Equip function from EquipmentManager, passing in the private gear object from this class. (Assumes equipment manager is initialized already)
    {
        EquipmentManager.Instance.Equip(gear);
    }

    private Color RarityColor(LootManager.GearRarity rarity)
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
}
