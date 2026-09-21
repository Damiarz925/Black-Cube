// Runtime-safe serialized contracts for editor-authored Passive Tree V3 content.
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PassiveIconMode { Auto, Custom }
public enum PassiveEffectKind { Stat, Mechanic }

[Serializable]
public sealed class PassiveAuthoredEffect
{
    [SerializeField] PassiveEffectKind kind = PassiveEffectKind.Stat;
    [SerializeField] StatTypes stat;
    [SerializeField] string mechanicId = string.Empty;
    [SerializeField] float value;

    public PassiveEffectKind Kind => kind;
    public StatTypes Stat => stat;
    public string MechanicId => mechanicId ?? string.Empty;
    public float Value => value;

    public void SetStat(StatTypes valueType, float amount) { kind = PassiveEffectKind.Stat; stat = valueType; mechanicId = string.Empty; value = amount; }
    public void SetMechanic(string stableMechanicId, float amount) { kind = PassiveEffectKind.Mechanic; mechanicId = stableMechanicId ?? string.Empty; value = amount; }
}

[Serializable]
public sealed class PassiveAuthoredNode
{
    [SerializeField] string stableId = string.Empty;
    [SerializeField] string logicalSlotId = string.Empty;
    [SerializeField] string displayName = string.Empty;
    [TextArea, SerializeField] string description = string.Empty;
    [SerializeField] PassiveBranch branch;
    [SerializeField] PassiveNodeSize size = PassiveNodeSize.Small;
    [SerializeField] PassiveNodeKind kind = PassiveNodeKind.Choice;
    [SerializeField] PassiveKeystone keystone;
    [SerializeField] List<PassiveAuthoredEffect> effects = new();
    [SerializeField] PassiveIconMode iconMode;
    [SerializeField] Sprite iconOverride;

    public string StableId => stableId ?? string.Empty;
    public string LogicalSlotId => string.IsNullOrEmpty(logicalSlotId) ? StableId : logicalSlotId;
    public string DisplayName => displayName ?? string.Empty;
    public string Description => description ?? string.Empty;
    public PassiveBranch Branch => branch;
    public PassiveNodeSize Size => size;
    public PassiveNodeKind Kind => kind;
    public PassiveKeystone Keystone => keystone;
    public IReadOnlyList<PassiveAuthoredEffect> Effects => effects;
    public PassiveIconMode IconMode => iconMode;
    public Sprite IconOverride => iconOverride;

    public void Configure(string id, string label, string tooltip, PassiveBranch authoredBranch, PassiveNodeSize authoredSize, PassiveNodeKind authoredKind, PassiveKeystone authoredKeystone, IEnumerable<PassiveEffect> authoredEffects, string slotId = null)
    {
        stableId = id ?? string.Empty; displayName = label ?? string.Empty; description = tooltip ?? string.Empty;
        logicalSlotId = string.IsNullOrEmpty(slotId) ? stableId : slotId;
        branch = authoredBranch; size = authoredSize; kind = authoredKind; keystone = authoredKeystone;
        effects.Clear();
        if (authoredEffects == null) return;
        foreach (PassiveEffect effect in authoredEffects) { var line = new PassiveAuthoredEffect(); line.SetStat(effect.Stat, effect.Amount); effects.Add(line); }
    }
}

[Serializable]
public sealed class PassiveChoiceSideData
{
    [SerializeField] PassiveAuthoredNode a = new();
    [SerializeField] PassiveAuthoredNode b = new();
    [SerializeField] PassiveAuthoredNode c = new();
    [SerializeField] PassiveAuthoredNode subclassA = new();
    [SerializeField] PassiveAuthoredNode subclassB = new();

    public PassiveAuthoredNode A => a;
    public PassiveAuthoredNode B => b;
    public PassiveAuthoredNode C => c;
    public PassiveAuthoredNode SubclassA => subclassA;
    public PassiveAuthoredNode SubclassB => subclassB;
    public IEnumerable<PassiveAuthoredNode> GenericNodes { get { yield return a; yield return b; yield return c; } }
}

[Serializable]
public sealed class PassiveClassTierData
{
    [SerializeField, Range(1, 10)] int tier = 1;
    [SerializeField] PassiveAuthoredNode spine = new();
    [SerializeField] PassiveChoiceSideData right = new();
    [SerializeField] PassiveChoiceSideData left = new();
    public int Tier => tier;
    public PassiveAuthoredNode Spine => spine;
    public PassiveChoiceSideData Right => right;
    public PassiveChoiceSideData Left => left;
    public void SetTier(int value) => tier = Mathf.Clamp(value, 1, 10);
}

[Serializable]
public sealed class PassiveWeaponTierData
{
    [SerializeField, Range(1, 5)] int tier = 1;
    [SerializeField] PassiveAuthoredNode spine = new();
    [SerializeField] PassiveChoiceSideData right = new();
    [SerializeField] PassiveChoiceSideData left = new();
    public int Tier => tier;
    public PassiveAuthoredNode Spine => spine;
    public PassiveChoiceSideData Right => right;
    public PassiveChoiceSideData Left => left;
    public void SetTier(int value) => tier = Mathf.Clamp(value, 1, 5);
}

[Serializable]
public sealed class PassiveBranchVisualStyle
{
    public Sprite spineNormal, spineHover, spineAllocated, spineUnavailable;
    public Sprite choiceNormal, choiceHover, choiceAllocated, choiceUnavailable;
    public Sprite subclassLocked, subclassAvailable, subclassHover, subclassAllocated;
    public Sprite junction;
    public Material connectionMaterial;
    public Color classAccent = new(.95f, .45f, .1f, 1f);
    public Color weaponAccent = new(.95f, .65f, .16f, 1f);
    public Color subclassAccent = new(.78f, .42f, 1f, 1f);
    public Sprite branchBackground;
}

public abstract class PassiveBranchDataSO : ScriptableObject
{
    [SerializeField] PassiveBranchVisualStyle visualStyle = new();
    public PassiveBranchVisualStyle VisualStyle => visualStyle;
    public abstract string RouteId { get; }
    public abstract int TierCount { get; }
    public abstract IEnumerable<PassiveAuthoredNode> AllAuthoredNodes();
}

[Serializable]
public sealed class PassiveMechanicDefinition
{
    [SerializeField] string stableId = string.Empty;
    [SerializeField] string displayName = string.Empty;
    [TextArea, SerializeField] string description = string.Empty;
    public string StableId => stableId ?? string.Empty;
    public string DisplayName => displayName ?? string.Empty;
    public string Description => description ?? string.Empty;
    public void Configure(string id, string label, string detail) { stableId = id ?? string.Empty; displayName = label ?? string.Empty; description = detail ?? string.Empty; }
}

[Serializable]
public sealed class PassiveIconMapping
{
    [SerializeField] StatTypes effect;
    [SerializeField] Sprite normal, hovered, allocated, unavailable;
    public StatTypes Effect => effect;
    public Sprite Normal => normal;
    public Sprite Resolve(PassiveNodeVisualState state) => state switch { PassiveNodeVisualState.Hover => hovered != null ? hovered : normal, PassiveNodeVisualState.Allocated => allocated != null ? allocated : normal, PassiveNodeVisualState.Unavailable => unavailable != null ? unavailable : normal, _ => normal };
    public void Configure(StatTypes stat, Sprite normalSprite, Sprite hoverSprite, Sprite allocatedSprite, Sprite unavailableSprite) { effect = stat; normal = normalSprite; hovered = hoverSprite; allocated = allocatedSprite; unavailable = unavailableSprite; }
}
