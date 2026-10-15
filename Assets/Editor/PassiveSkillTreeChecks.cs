using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PassiveSkillTreeChecks
{
    [MenuItem("Black Cube/Checks/Verify Passive Skill Tree Layout")]
    public static void Verify()
    {
        Require(PassiveSkillTreeView.Categories.Length == 10, "Expected ten passive categories.");
        Require(PassiveSkillTreeView.Phases.Length == 12, "Expected twelve nodes per branch.");
        Require(Mathf.Approximately(PassiveSkillTreeView.BranchAngleStep, 36f), "Branches must be 36 degrees apart.");

        var expected = new[] { "Damage", "Life", "Defense", "Speed", "Magic", "Projectile", "Cold/Nature", "Fire", "Lightning", "Minion" };
        Require(PassiveSkillTreeView.Categories.Select(value => value.Name).SequenceEqual(expected), "Category order or names changed.");

        for (var i = 0; i < 5; i++)
            Require(PassiveSkillTreeView.Phases[i].Size == PassiveSkillTreeView.NodeSize.Small, "Nodes 1-5 must be small.");
        Require(PassiveSkillTreeView.Phases[5].Size == PassiveSkillTreeView.NodeSize.Large, "Node 6 must be large.");
        for (var i = 6; i < 11; i++)
            Require(PassiveSkillTreeView.Phases[i].Size == PassiveSkillTreeView.NodeSize.Medium, "Nodes 7-11 must be medium.");
        Require(PassiveSkillTreeView.Phases[11].Size == PassiveSkillTreeView.NodeSize.Large, "Terminal node must be large.");

        for (var i = 1; i < PassiveSkillTreeView.Phases.Length; i++)
            Require(PassiveSkillTreeView.Phases[i].Radius > PassiveSkillTreeView.Phases[i - 1].Radius, "Node radii must increase strictly outward.");

        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/PassiveSkillTree/PassiveTreeIcons.png");
        Require(atlas != null && atlas.width == 1536 && atlas.height == 1024, "PassiveTreeIcons atlas is missing or has unexpected dimensions.");
        Debug.Log("PASS: passive tree has 10 equidistant 36-degree branches; each has 5 small, 1 large, 5 medium, and 1 terminal large node; category atlas is present.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
