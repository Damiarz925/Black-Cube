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
        writer.WriteLine("Step 15 world-content validation");
        writer.WriteLine("biomes=6 locations-per-biome=10 corruption-tiers=6 mapped-levels=360");
        writer.WriteLine("reference-normal=enemy.goblin reference-boss=boss.hobgoblin");
        writer.WriteLine("placeholder-biomes=6 production-complete=false");
        foreach (string error in errors) writer.WriteLine("FAIL " + error);
        writer.WriteLine($"RESULT: {(errors.Count == 0 ? "PASS" : "FAIL")} ({errors.Count} errors)");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
    }
}
