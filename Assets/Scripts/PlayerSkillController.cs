// Developer map: Owns the single equipped active skill and its transient next-attack queue.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StatsComponent), typeof(ManaComponent))]
public sealed class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerSkillCatalog catalog;
    [SerializeField] private List<PlayerSkillDefinition> skills = new();
    public IReadOnlyList<PlayerSkillDefinition> Skills => skills;
    public PlayerSkillDefinition SelectedSkill { get; private set; }
    public PlayerSkillDefinition QueuedSkill { get; private set; }
    public bool HasQueuedSkill => QueuedSkill != null;
    public ManaComponent Mana { get; private set; }
    public event System.Action SelectionChanged;
    public event System.Action QueueChanged;

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
        ClearQueuedSkill();
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
        ClearQueuedSkill();
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

    public bool TryQueueSelected()
    {
        if (SelectedSkill == null || BattleManager.Instance == null) return false;
        float cost = ManaCost(SelectedSkill);
        if (!BattleManager.Instance.CanCastPlayerSkill || !Mana.CanSpend(cost)) return false;
        if (QueuedSkill == SelectedSkill) return true;
        QueuedSkill = SelectedSkill;
        QueueChanged?.Invoke();
        return true;
    }

    // Compatibility entry point retained for existing UI/tests; skills no longer cast instantly.
    public bool TryCastSelected() => TryQueueSelected();

    public bool TryConsumeQueuedForAttack(out PlayerSkillDefinition skill, out float manaSpent)
    {
        skill = QueuedSkill;
        manaSpent = 0f;
        if (skill == null) return false;
        QueuedSkill = null;
        QueueChanged?.Invoke();
        float cost = ManaCost(skill);
        if (!Mana.TrySpend(cost)) { skill = null; return false; }
        manaSpent = cost;
        return true;
    }

    public void ClearQueuedSkill()
    {
        if (QueuedSkill == null) return;
        QueuedSkill = null;
        QueueChanged?.Invoke();
    }
}
