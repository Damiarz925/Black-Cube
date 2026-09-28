using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Generates documentation from the same assets/rules used by production.
public static class ClassKeystoneContractExport
{
    public static void VerifyAndCapture()
    {
        PassiveTreeV3Validation.RunStructure();
        var errors=UIAuthoringValidation.ValidateAll();
        if(errors.Length!=0)throw new InvalidOperationException(string.Join("\n",errors));
        Export();GenericClassPassiveVerification.Capture();
        Debug.Log("CLASS KEYSTONE STRUCTURE / UI BINDINGS / SIX-CLASS CAPTURES: PASS");
    }
    public static void Export()
    {
        var database=Resources.Load<PassiveTreeDatabaseSO>("GameData/PassiveTree/SO_PassiveTreeDatabase");
        var text=new StringBuilder("# Class keystone and weapon-tree content contract\n\nPROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE\n\n");
        text.AppendLine("Save schema 13. One level-owned passive budget; 100 points at level 100. Native 30 nodes plus one keystone unlock explicit additional-class and single-weapon selection. Off-class travel requires ten spines; its keystone additionally requires twenty side choices. Every weapon tree costs at most 21 shared points.\n");
        text.AppendLine("## Eighteen class keystones\n\n| Class | Keystone | Exact effect |\n|---|---|---|");
        foreach(var key in ClassKeystoneCatalog.All)text.AppendLine($"| {key.classId} | {key.name} | {key.description} |");
        text.AppendLine("\nThese are first-pass values. High thresholds, target conditions, resource pressure and damage-type restrictions can produce weak or inactive results in the controlled survey; that is a diagnostic, not permission to tune them automatically.\n");
        foreach(var weapon in database.WeaponBranches)
        {
            var profile=ProvisionalWeaponTreeContent.For(weapon.WeaponId);
            string Names(StatTypes[] stats)=>string.Join(", ",stats.Select(StatDisplayFormatting.ToFriendlyName));
            string Effects(PassiveAuthoredNode node)=>string.Join("; ",node.Effects.Select(e=>$"{StatDisplayFormatting.ToFriendlyName(e.Stat)} {StatsComponent.ToDisplayedValue(e.Stat,e.Value):+0.###;-0.###;0}{(StatsComponent.IsPercentStat(e.Stat)?"%":"")}"));
            text.AppendLine($"## {weapon.WeaponId} — PROVISIONAL\n\nOffense: {Names(profile.Offense)}.\n\nDefense: {Names(profile.Defense)}.\n\nUtility: {Names(profile.Utility)}.\n");
            text.AppendLine("Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.\n");
            text.AppendLine("| Tier | Left A | Left B | Left C | Right A | Right B | Right C |\n|---:|---|---|---|---|---|---|");
            foreach(var tier in weapon.Tiers)
            {
                var choices=tier.Left.GenericNodes.Concat(tier.Right.GenericNodes).Select(n=>Effects(n)+(n.Keystone==PassiveKeystone.RageFinisher?"; at full Rage arm one 2× attack event, consuming Rage after resolution":""));
                text.AppendLine($"| {tier.Tier} | {string.Join(" | ",choices)} |");
            }
            text.AppendLine("\nOnly one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.\n");
        }
        text.AppendLine("## Weapon-exclusive suffix ladders\n\nAmong weapons only: Sword Multistrike, Bow Projectiles, Staff Cooldown Reduction, Sceptre Aura Effect, Axe local Physical MORE plus global Rage Generation, Dagger Culling. Nonweapon and passive sources are retained.\n\n| Weapon | Family | Tier | Minimum item level | Primary range | Secondary range |\n|---|---|---:|---:|---|---|");
        foreach(StatTypes stat in Enum.GetValues(typeof(StatTypes)))
        {
            string weapon=WeaponExclusiveAffixRules.Weapon(stat);if(weapon==null)continue;
            WeaponExclusiveAffixRules.TryGet(stat,LootManager.GearType.Weapons,weapon,out var tiers);
            foreach(var tier in tiers)
                text.AppendLine($"| {weapon} | {StatDisplayFormatting.ToFriendlyName(stat)} | T{tier.tierIndex} | {tier.minItemLevel} | {tier.minValue:0.###}–{tier.maxValue:0.###}{(stat==StatTypes.ProjectileAmount?" projectiles":"%")} | {(tier.pairedDamage?$"{tier.minHighValue:0.###}–{tier.maxHighValue:0.###}% Rage Generation":"—")} |");
        }
        text.AppendLine("\nAxe Physical MORE scales local Physical before global scaling; it does not scale local Fire/Cold/Lightning/Void. Dagger Culling checks current Life against maximum Life and includes bosses. All exclusive tiers begin at item level 50, then 60/70/80/90, with T1 strongest. Each tier has weight 100. Five rare Amulet aura-access prefixes remain unchanged.\n");
        text.AppendLine("## Units and compatibility\n\nLife Regeneration is percentage maximum Life/sec, Mana Regeneration flat Mana/sec, Physical Damage Reduction fractional (0.02 = 2%). Schema-12 migration refunds passive allocations, clears selected specialization/routes and explicitly converts historical regeneration rolls. Other run/class/subclass/equipment/rebirth progression is preserved.\n\nSee CLASS_KEYSTONE_SYSTEM_GUIDE.md for UI navigation, authoring sockets, staged save, combat rules and survey methodology. Validation and results are reported separately; this content export alone does not certify completion.");
        Directory.CreateDirectory("Docs");File.WriteAllText("Docs/CLASS_KEYSTONE_CONTENT_CONTRACT.md",text.ToString());
        Debug.Log("CLASS KEYSTONE CONTENT CONTRACT: EXPORTED");
    }
}
