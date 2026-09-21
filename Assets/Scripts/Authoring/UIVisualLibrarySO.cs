using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Black-Cube/UI/Visual Library", fileName = "SO_UIVisualLibrary")]
public sealed class UIVisualLibrarySO : ScriptableObject
{
    [Header("Fallbacks")] public Sprite missingSprite;
    [Header("Major Panels")] public Sprite hudPanel, inventoryBackground, tooltipBackground, passiveTreeBackground, pauseBackground, menuBackground;
    [Header("Rarity Frames")] public Sprite normalFrame, magicFrame, rareFrame, legendaryFrame;
    [Header("Button Styles")] public UIButtonVisualStyleSO defaultButtonStyle, hudButtonStyle, inventoryToggleStyle;
    [Header("HUD Button Art")] public List<HUDButtonVisualSet> hudButtons = new();
    [Header("Currency")] public List<Sprite> ordinaryCurrencySprites = new(), ancientCurrencySprites = new();
    [Header("Weapons")] public Sprite sword, twoHandedAxe, bow, staff, dagger, sceptre;
    [Header("Status")] public Sprite freeze, poison, bleed, ignite, shock, chill;
    public HUDButtonVisualSet HUDButton(TopHUDButtonKind kind) { foreach (HUDButtonVisualSet set in hudButtons) if (set != null && set.kind == kind) return set; return null; }
}
