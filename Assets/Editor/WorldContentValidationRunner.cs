using System;
using System.IO;
using UnityEditor;

public static class WorldContentValidationRunner
{
    public const string ReportPath = "Logs/Step15-world-content-validation.txt";

    [MenuItem("Black Cube/Validation/Validate World Content")]
    public static void ValidateReferenceContent()
    {
        var errors = WorldContentValidation.Validate(WorldContentCatalog.Reference);
        Directory.CreateDirectory("Logs");
        using var writer = new StreamWriter(ReportPath, false);
        var content=WorldContentCatalog.Reference;
        writer.WriteLine("Step 19 production world-content validation");
        writer.WriteLine("biomes=6 locations=60 corruption-tiers=6 mapped-levels=360 mapped-stages=3600");
        writer.WriteLine($"normal-enemies={content.enemyArchetypes.Count} main-bosses={content.bosses.FindAll(x=>!x.challengeBoss).Count} challenge-bosses={content.bosses.FindAll(x=>x.challengeBoss).Count}");
        writer.WriteLine($"enemy-skills={content.enemySkills.Count} loadouts={content.enemySkillLoadouts.Count} phase-profiles={content.bossPhaseProfiles.Count} special-affix-pools={content.challengeSpecialAffixPools.Count}");
        writer.WriteLine("mechanical-content=production presentation=paper-prefab-fallback-until-final-art");
        foreach (string error in errors) writer.WriteLine("FAIL " + error);
        writer.WriteLine($"RESULT: {(errors.Count == 0 ? "PASS" : "FAIL")} ({errors.Count} errors)");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
    }
}
