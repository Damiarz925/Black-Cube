// Developer map: Resolves item-level/theme/set bands to the extracted inventory
// icon resources. Levels above 300 remain on corrupted City set 3 (endgame).
using System.Collections.Generic;
using UnityEngine;

public static class ItemIconCatalog
{
    private static readonly string[] Themes = { "forest", "desert", "tundra", "volcanic", "city" };
    private static readonly int[] CorruptionPercentages = { 0, 20, 40, 60, 80, 100 };
    private static readonly Dictionary<string, Sprite> Cache = new();
    private static Sprite highlightBorder;

    public readonly struct Selection
    {
        public readonly string Theme;
        public readonly int Set;
        public readonly bool Corrupted;
        public Selection(string theme, int set, bool corrupted)
        { Theme=theme; Set=set; Corrupted=corrupted; }
    }

    public static Selection ForLevel(int itemLevel)
    {
        int level = Mathf.Clamp(itemLevel, 1, 300);
        int fiftyLevelBand = (level-1)/50;
        int themeIndex = ((level-1)%50)/10;
        return new Selection(Themes[themeIndex], fiftyLevelBand%3+1, fiftyLevelBand>=3);
    }

    public static Sprite Get(Gear gear) => gear == null || gear.IsScrap ? null : Get(gear.ItemType, gear.ItemLevel);

    public static Sprite Get(LootManager.GearType type, int itemLevel)
    {
        Selection selection = ForLevel(itemLevel);
        string variant = selection.Corrupted ? "corrupted_"+selection.Theme : selection.Theme;
        string path = $"UI/ItemIcons/{variant}/set_{selection.Set}/{FileName(type)}";
        return Load(path);
    }

    public static Sprite HighlightBorder
    {
        get
        {
            if (highlightBorder == null)
                highlightBorder = Load("UI/ItemIcons/mod_highlight_border");
            return highlightBorder;
        }
    }

    public static Sprite GetCorruptionBorder(int stage)
    {
        int percentage = CorruptionPercentages[Mathf.Clamp(stage, 0, CorruptionPercentages.Length-1)];
        return Load($"UI/ItemIcons/corruption_border_{percentage}");
    }

    private static Sprite Load(string path)
    {
        if (Cache.TryGetValue(path, out Sprite cached)) return cached;
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning("ItemIconCatalog: missing icon resource " + path);
            Cache[path] = null;
            return null;
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        var sprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), texture.width);
        sprite.name = path.Substring(path.LastIndexOf('/')+1);
        Cache[path] = sprite;
        return sprite;
    }

    private static string FileName(LootManager.GearType type) => type switch
    {
        LootManager.GearType.Helmets => "helmet",
        LootManager.GearType.BodyArmours => "body",
        LootManager.GearType.Gloves => "gloves",
        LootManager.GearType.Boots => "boots",
        LootManager.GearType.Amulets => "amulet",
        LootManager.GearType.Rings => "ring",
        LootManager.GearType.Belts => "belt",
        _ => "sword"
    };
}
