// Developer map: Owns the single equipped active skill and spends mana before BattleManager casts it.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StatsComponent), typeof(ManaComponent))]
public sealed class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerSkillCatalog catalog;
    [SerializeField] private List<PlayerSkillDefinition> skills = new();
    public IReadOnlyList<PlayerSkillDefinition> Skills => skills;
    public PlayerSkillDefinition SelectedSkill { get; private set; }
    public ManaComponent Mana { get; private set; }
    public event System.Action SelectionChanged;

    private void Awake()
    {
        Mana = GetComponent<ManaComponent>();
        if (catalog == null) catalog = Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        if (catalog != null && catalog.skills != null && catalog.skills.Count > 0)
            skills = catalog.skills;
        if (skills == null || skills.Count == 0) skills = PlayerSkillDefinition.CreateDefaults();
    }

    public bool TrySelect(PlayerSkillDefinition skill)
    {
        if (skill == null || SelectedSkill != null || !skills.Contains(skill)) return false;
        SelectedSkill = skill;
        SelectionChanged?.Invoke();
        GamePersistence.MarkDirty();
        return true;
    }

    public bool TryForfeit(PlayerSkillDefinition skill)
    {
        if (skill == null || SelectedSkill != skill) return false;
        SelectedSkill = null;
        SelectionChanged?.Invoke();
        GamePersistence.MarkDirty();
        return true;
    }

    public bool RestoreSelection(bool hasSelection, PlayerSkillId id)
    {
        PlayerSkillDefinition next = null;
        if (hasSelection)
        {
            foreach (var skill in skills) if (skill != null && skill.id == id) { next = skill; break; }
            if (next == null) return false;
        }
        SelectedSkill = next;
        SelectionChanged?.Invoke();
        return true;
    }

    public bool HasSkill(PlayerSkillId id)
    {
        foreach (var skill in skills) if (skill != null && skill.id == id) return true;
        return false;
    }

    public float ManaCost(PlayerSkillDefinition skill)
    {
        if (skill == null) return 0f;
        int level = EffectiveSkillLevel(skill);
        float cost = Mathf.Max(0f, skill.manaCost) * ManaCostLevelFactor(level);
        var keystones = GetComponent<PassiveKeystoneState>();
        cost *= keystones != null ? keystones.ManaCostMultiplier : 1f;
        return Mathf.Round(cost);
    }

    public int EffectiveSkillLevel(PlayerSkillDefinition skill)
    {
        if (skill == null) return 1;
        StatsComponent stats = GetComponent<StatsComponent>();
        float added=stats != null ? stats.GetRawStat(SkillLevelStat(skill.id)) : 0f;
        if(skill==SelectedSkill)added+=RelicInventory.Instance?.EquippedSkillLevelBonus??0;
        return CalculateEffectiveSkillLevel(added);
    }

    public static int CalculateEffectiveSkillLevel(float addedLevels) =>
        Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Max(0f, addedLevels)), 1, 20);

    public static float SkillDamageLevelFactor(int effectiveLevel) =>
        1f + .05f * (Mathf.Clamp(effectiveLevel, 1, 20) - 1);

    public static float ManaCostLevelFactor(int effectiveLevel) =>
        1f + .02f * (Mathf.Clamp(effectiveLevel, 1, 20) - 1);

    public static StatTypes SkillLevelStat(PlayerSkillId id) => id switch
    {
        PlayerSkillId.HeavyStrike => StatTypes.Plus1Phys,
        PlayerSkillId.IceStrike => StatTypes.Plus1Cold,
        PlayerSkillId.LightningStrike => StatTypes.Plus1Light,
        PlayerSkillId.Fireball => StatTypes.Plus1Fire,
        PlayerSkillId.Envenom => StatTypes.Plus1Poison,
        PlayerSkillId.Shiv => StatTypes.Plus1Bleed,
        PlayerSkillId.Immolate => StatTypes.Plus1Ignite,
        _ => StatTypes.Plus1Phys
    };

    public bool TryCastSelected()
    {
        if (SelectedSkill == null || BattleManager.Instance == null) return false;
        float cost = ManaCost(SelectedSkill);
        if (!BattleManager.Instance.CanCastPlayerSkill || !Mana.CanSpend(cost)) return false;
        if (!Mana.TrySpend(cost)) return false;
        if (BattleManager.Instance.TryCastPlayerSkill(SelectedSkill)) return true;
        Mana.Restore(cost);
        return false;
    }
}
