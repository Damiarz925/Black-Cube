using UnityEngine;

public static class EquipmentSlotRules
{
    public static bool IsWeaponSlot(LootManager.GearType type)=>type is LootManager.GearType.Weapons or LootManager.GearType.OffHands;
    public static LootManager.GearType AffixSlot(LootManager.GearType type)=>IsWeaponSlot(type)?LootManager.GearType.Weapons:type;
    public static bool IsTwoHanded(string weaponTypeId)=>weaponTypeId is WeaponTypeIds.TwoHandedAxe or WeaponTypeIds.Bow or WeaponTypeIds.Staff;
    public static bool CanEquipOffHand(Gear gear)=>gear!=null&&IsWeaponSlot(gear.ItemType)&&!IsTwoHanded(gear.WeaponTypeId);
    public static Vector2Int InventorySize(LootManager.GearType type)=>type switch
    {
        LootManager.GearType.Weapons or LootManager.GearType.OffHands or LootManager.GearType.BodyArmours=>new Vector2Int(2,3),
        LootManager.GearType.Helmets or LootManager.GearType.Gloves or LootManager.GearType.Boots=>new Vector2Int(2,2),
        LootManager.GearType.Belts=>new Vector2Int(2,1),
        _=>Vector2Int.one
    };
}
