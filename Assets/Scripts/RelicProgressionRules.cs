using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// One first-pass progression profile, shared by reward preview, live Rebirth and tests.
public static class RelicProgressionRules
{
    public static int RewardCount(int reached)=>Mathf.Clamp(1+(Mathf.Max(60,reached)-60)/40,1,8);
    public static Vector4 RarityWeights(int reached)
    {
        float t=Mathf.Clamp01(((Mathf.Max(60,reached)-60)/10)*10f/280f);
        return Vector4.Lerp(new Vector4(100,0,0,0),new Vector4(5,15,40,40),t);
    }
    public static LootManager.GearRarity RollRarity(int reached,float roll)
    {
        var weights=RarityWeights(reached);float choice=Mathf.Clamp01(roll)*100;
        for(int i=0;i<3;i++){choice-=weights[i];if(choice<0)return (LootManager.GearRarity)i;}
        return reached<=60?LootManager.GearRarity.Normal:LootManager.GearRarity.Legendary;
    }
    public static RelicData Generate(LootManager.GearRarity rarity,int level,int cycle)
    {
        var relic=new RelicData{id=Guid.NewGuid().ToString("N"),cycle=Mathf.Max(1,cycle),rarity=rarity,
            relicLevel=Mathf.Clamp(level,1,100),craftableThisCycle=true};
        int count=UnityEngine.Random.Range(AncientRelicCrafting.Minimum(rarity),AncientRelicCrafting.Maximum(rarity)+1);
        var used=new HashSet<RelicModifierType>();
        for(int i=0;i<count;i++){var mod=RelicRolls.Roll(relic.relicLevel,i==0,used);relic.modifiers.Add(mod);used.Add(mod.type);}
        return relic;
    }
    public static Element ClassElement(string classId)=>classId switch
    {PlayerClassIds.Ranger=>Element.Void,PlayerClassIds.Mage=>Element.Fire,PlayerClassIds.Priest=>Element.Cold,_=>Element.Phys};
    public static string WeaponUnlock(RelicModifierType type)=>type switch
    {RelicModifierType.StarterSword=>WeaponTypeIds.Sword,RelicModifierType.StarterAxe=>WeaponTypeIds.TwoHandedAxe,
     RelicModifierType.StarterBow=>WeaponTypeIds.Bow,RelicModifierType.StarterStaff=>WeaponTypeIds.Staff,
     RelicModifierType.StarterSceptre=>WeaponTypeIds.Sceptre,RelicModifierType.StarterDagger=>WeaponTypeIds.Dagger,_=>null};
    public static PlayerSkillId? TriggerSkill(RelicModifierType type)=>type switch
    {RelicModifierType.TriggerRapidFlurry=>PlayerSkillId.SwordRendingStrike,RelicModifierType.TriggerArmourStrike=>PlayerSkillId.SwordArmourStrike,
     RelicModifierType.TriggerRageStrike=>PlayerSkillId.AxeRageStrike,RelicModifierType.TriggerHemorrhage=>PlayerSkillId.AxeHemorrhage,
     RelicModifierType.TriggerVenomShot=>PlayerSkillId.BowVenomShot,RelicModifierType.TriggerDoubleVolley=>PlayerSkillId.BowDoubleVolley,
     RelicModifierType.TriggerFireball=>PlayerSkillId.StaffFireball,RelicModifierType.TriggerShockBarrage=>PlayerSkillId.StaffShockBarrage,
     RelicModifierType.TriggerRestorativeStrike=>PlayerSkillId.SceptreRestorativeStrike,RelicModifierType.TriggerFrostJudgment=>PlayerSkillId.SceptreFrostJudgment,
     RelicModifierType.TriggerBackstab=>PlayerSkillId.DaggerBackstab,RelicModifierType.TriggerQuickStrike=>PlayerSkillId.DaggerQuickStrike,_=>null};
    // Retired serialized identities are never reused. Preserve IDs/rarity/levels and replace payloads deterministically.
    public static void MigrateLegacy(RelicData relic)
    {
        if(relic?.modifiers==null)return;
        relic.crafted=true; // Legacy saves did not record crafting history: conservatively prohibit fusion.
        var used=new HashSet<RelicModifierType>();
        foreach(var mod in relic.modifiers)if(mod!=null&&!RelicModifierDefinitions.IsRetired(mod.type))used.Add(mod.type);
        for(int i=0;i<relic.modifiers.Count;i++)
        {
            var mod=relic.modifiers[i];if(mod==null)continue;mod.lockedOriginal=i==0;
            if(!RelicModifierDefinitions.IsRetired(mod.type))continue;
            var replacement=mod.type==RelicModifierType.ShockThresholdReduction?RelicModifierType.MaximumShockEffect:RelicModifierType.MoreEnemyDrops;
            if(used.Contains(replacement))replacement=RelicModifierDefinitions.All.First(d=>!used.Contains(d.Id)).Id;
            used.Add(replacement);mod.type=replacement;
            var definition=RelicModifierDefinitions.Get(replacement);
            var tier=definition.Tiers.FirstOrDefault(t=>t.TierIndex==mod.tierIndex&&t.MinimumRelicLevel<=relic.relicLevel)??definition.Tiers[0];
            mod.tierIndex=tier.TierIndex;mod.value=(tier.Minimum+tier.Maximum)*.5f;
        }
    }
}

public sealed partial class RelicInventory
{
    public IReadOnlyList<string> StartingWeaponChoices(string classDefault)
    {
        var result=new List<string>{classDefault};foreach(var relic in ActiveRelics())foreach(var mod in relic.modifiers)
        {string id=RelicProgressionRules.WeaponUnlock(mod.type);if(id!=null&&!result.Contains(id))result.Add(id);}return result;
    }
    public Element ResolveStarterElement(Element classDefault)
    {return ActiveRelics().Any(r=>r.modifiers.Any(m=>IsStarterElement(m.type)))?StarterElement:classDefault;}
    public IReadOnlyList<int> ReturnItemLevelLimits()=>ActiveRelics().SelectMany(r=>r.modifiers).Where(m=>m.type==RelicModifierType.RebirthItemReturn).Select(m=>Mathf.FloorToInt(m.value)).ToArray();
    public IEnumerable<PlayerSkillId> TriggeredSkills()=>ActiveRelics().SelectMany(r=>r.modifiers).Select(m=>RelicProgressionRules.TriggerSkill(m.type)).Where(id=>id.HasValue).Select(id=>id.Value).Distinct();
    public void Grant(RelicData relic){if(relic==null||relics.Any(r=>r.id==relic.id))return;currentCycle=Mathf.Max(currentCycle,relic.cycle);relics.Add(relic);PublishChanged();}
    public bool TryFuse(IReadOnlyList<RelicData> inputs,out RelicData result)
    {
        result=null;if(GameManager.Instance!=null&&RebirthManager.Instance?.Phase!=RebirthPhase.Crafting)return false;if(inputs==null||inputs.Count!=5||inputs.Any(r=>r==null)||inputs.Distinct().Count()!=5)return false;
        var rarity=inputs[0].rarity;
        if(rarity>=LootManager.GearRarity.Legendary||inputs.Any(r=>!relics.Contains(r)||r.uniqueRelic||r.rarity!=rarity||!r.Pristine))return false;
        // Generate before changing ownership; invalid recipes never partially consume materials.
        result=RelicProgressionRules.Generate((LootManager.GearRarity)((int)rarity+1),Mathf.RoundToInt((float)inputs.Average(r=>r.relicLevel)),Mathf.Max(1,currentCycle));
        var active=new RelicData[ActiveSlotCount];for(int i=0;i<active.Length;i++)active[i]=Active(i);
        foreach(var input in inputs)relics.Remove(input);relics.Add(result);
        for(int i=0;i<active.Length;i++)activeIndices[i]=active[i]==null?-1:relics.IndexOf(active[i]);
        PublishChanged();return true;
    }
}
