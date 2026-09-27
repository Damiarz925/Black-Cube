using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCube.BalanceWorkbench
{
    [Serializable] public sealed class RealisticInventoryResult
    {
        public long seed;
        public int observedDrops,generatedItems;
        public List<GearSnapshot> items=new();
        public List<ProgressionCurrencyCount> currencies=new();
        public string warning;
    }

    public static class RealisticInventoryGenerator
    {
        // The same item-type, element, rarity, affix and weapon-profile functions as
        // LootManager.GenerateLoot, but without a scene ZoneManager or world objects.
        public static RealisticInventoryResult Generate(ProgressionHistoryResult history,long seed)
        {
            if(history==null)throw new ArgumentNullException(nameof(history));
            if(history.encounters==null||history.encounters.Count==0)
                throw new InvalidOperationException("A non-manual progression history is required to generate inventory.");
            var rng=new SeededSimulationRandomSource(seed);
            var result=new RealisticInventoryResult{seed=seed,observedDrops=(int)Math.Round(history.gearDrops),
                currencies=history.currencies.Select(x=>new ProgressionCurrencyCount{currency=x.currency,count=x.count}).ToList()};
            var lootObject=new GameObject("Isolated production loot policy"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                var loot=lootObject.AddComponent<LootManager>();
                using var session=new WorkbenchSession();
                foreach(var encounter in history.encounters)
                {
                    // Expected histories use stochastic rounding per historical encounter.
                    int copies=encounter.gearDrops==Math.Floor(encounter.gearDrops)?
                        (int)encounter.gearDrops:LootDropBalanceProfileSO.RollCopies((float)encounter.gearDrops,rng);
                    for(int i=0;i<copies;i++)
                    {
                        var slot=loot.RollItemType(rng);
                        int itemLevel=Mathf.Clamp(encounter.itemLevel,1,100);
                        var rarity=loot.RollItemRarity(itemLevel,rng);
                        var element=loot.RollItemElement(slot==LootManager.GearType.Weapons,rng);
                        string weapon=slot==LootManager.GearType.Weapons?LootManager.RollWeaponTypeId(rng):null;
                        var gear=session.ReusableItem;
                        gear.Initialize(slot,rarity,itemLevel,element,weapon);
                        List<RolledMod> mods=null;
                        for(int attempt=0;attempt<64&&mods==null;attempt++)
                            mods=session.Roller.RollEquipmentModsForItem(slot,rarity,itemLevel,element,
                                gear.WeaponTypeId,rng);
                        if(mods==null)throw new InvalidOperationException($"Production affix roll failed for {rarity} {slot} at item level {itemLevel}.");
                        gear.ApplyMods(mods);
                        if(slot==LootManager.GearType.Weapons)LootManager.ApplyNaturalWeaponProfile(gear);
                        result.items.Add(GearSnapshot.Capture(gear));
                    }
                }
                result.generatedItems=result.items.Count;
                result.warning="Historical items retain their original item levels. No crafting has been applied; this is Ground Loot Only.";
                return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(lootObject);}
        }
    }
}
