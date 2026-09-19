// Developer map: Owns the single equipped active skill and its transient next-attack queue.
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StatsComponent), typeof(ManaComponent))]
public sealed class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private PlayerSkillCatalog catalog;
    [SerializeField] private List<PlayerSkillDefinition> skills = new();
    private readonly List<PlayerSkillDefinition> weaponSkills = new(2);
    readonly float[] autoCooldownRemaining={0f,0f};
    public IReadOnlyList<PlayerSkillDefinition> Skills => skills;
    public IReadOnlyList<PlayerSkillDefinition> WeaponSkills => weaponSkills;
    public PlayerSkillDefinition SelectedSkill { get; private set; }
    public PlayerSkillDefinition QueuedSkill { get; private set; }
    public bool HasQueuedSkill => QueuedSkill != null;
    public ManaComponent Mana { get; private set; }
    public event System.Action SelectionChanged;
    public event System.Action QueueChanged;
    public event System.Action WeaponSkillsChanged;
    public event System.Action CooldownsChanged;
    private PlayerController player;

    private void Awake()
    {
        Mana = GetComponent<ManaComponent>();
        if (catalog == null) catalog = Resources.Load<PlayerSkillCatalog>("PlayerSkills");
        if (catalog != null && catalog.skills != null && catalog.skills.Count > 0)
            skills = catalog.skills;
        if (skills == null || skills.Count == 0 || !ContainsProductionSkills(skills)) skills = PlayerSkillDefinition.CreateProductionDefaults();
        player=GetComponent<PlayerController>();
        if(player!=null)player.AttackChanged+=RefreshWeaponSkills;
        RefreshWeaponSkills();
    }

    private void OnDestroy(){if(player!=null)player.AttackChanged-=RefreshWeaponSkills;}
    private void Update()=>TickAutoCooldowns(Time.deltaTime);

    public void RefreshWeaponSkills()
    {
        string weaponTypeId=player?.EquippedWeapon?.WeaponTypeId;
        var ids=WeaponSkillBindings.For(weaponTypeId);
        var next=new List<PlayerSkillDefinition>(2);
        foreach(var id in ids)
        {foreach(var definition in skills)if(definition!=null&&definition.id==id){next.Add(definition);break;}if(next.Count==2)break;}
        bool changed=next.Count!=weaponSkills.Count;
        if(!changed)for(int i=0;i<next.Count;i++)if(next[i]!=weaponSkills[i]){changed=true;break;}
        if(!changed)return;
        weaponSkills.Clear();weaponSkills.AddRange(next);ClearQueuedSkill();
        for(int i=0;i<autoCooldownRemaining.Length;i++)autoCooldownRemaining[i]=i<weaponSkills.Count&&weaponSkills[i].castMode==PlayerSkillCastMode.AutoCooldown?EffectiveCooldown(weaponSkills[i]):0f;
        WeaponSkillsChanged?.Invoke();CooldownsChanged?.Invoke();
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

    public bool TryQueueWeaponSkill(int index)
    {
        if(index<0||index>=weaponSkills.Count||BattleManager.Instance==null)return false;
        PlayerSkillDefinition skill=weaponSkills[index];float cost=ManaCost(skill);
        if(skill.castMode==PlayerSkillCastMode.ImmediateCooldown)return TryCastImmediate(index);
        if(skill.castMode!=PlayerSkillCastMode.QueuedAttackReplacement)return false;
        if(!BattleManager.Instance.CanCastPlayerSkill||!Mana.CanSpend(cost))return false;
        if(QueuedSkill==skill)return true;
        QueuedSkill=skill;GetComponent<SubclassCombatState>()?.ResetQueuedRepeats();QueueChanged?.Invoke();return true;
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

    public void NotifyQueuedSkillResolved(PlayerSkillDefinition skill)
    {
        if(skill==null||skill.castMode!=PlayerSkillCastMode.QueuedAttackReplacement)return;
        var subclass=GetComponent<SubclassCombatState>();
        if(subclass!=null&&subclass.TryQueueRepeat()){QueuedSkill=skill;QueueChanged?.Invoke();}
    }

    public void ClearQueuedSkill()
    {
        if (QueuedSkill == null) return;
        QueuedSkill = null;
        QueueChanged?.Invoke();
    }

    public const float MinimumAutoCooldown=.20f;
    public float EffectiveCooldown(PlayerSkillDefinition skill)
    {
        if(skill==null)return 0f;float speed=skill.scalesWithCooldownReduction?Mathf.Max(0f,GetComponent<StatsComponent>().GetStat(StatTypes.CooldownReduction)):skill.scalesWithCastSpeed?Mathf.Max(0f,GetComponent<StatsComponent>().GetStat(StatTypes.CastSpeed)):0f;
        return Mathf.Max(MinimumAutoCooldown,Mathf.Max(.01f,skill.baseCooldown)/(1f+speed));
    }
    public float CooldownRemaining(int slot)=>slot>=0&&slot<autoCooldownRemaining.Length?autoCooldownRemaining[slot]:0f;
    public bool AutoSkillReady(int slot)=>slot>=0&&slot<weaponSkills.Count&&weaponSkills[slot].castMode==PlayerSkillCastMode.AutoCooldown&&autoCooldownRemaining[slot]<=0f;
    public bool ImmediateSkillReady(int slot)=>slot>=0&&slot<weaponSkills.Count&&weaponSkills[slot].castMode==PlayerSkillCastMode.ImmediateCooldown&&autoCooldownRemaining[slot]<=0f;
    public bool TryCastImmediate(int slot)
    {
        if(!ImmediateSkillReady(slot)||BattleManager.Instance==null||!BattleManager.Instance.CanCastPlayerSkill)return false;
        var skill=weaponSkills[slot];float cost=ManaCost(skill);if(!Mana.TrySpend(cost))return false;
        if(!BattleManager.Instance.TryCastImmediatePlayerSkill(skill)){Mana.Restore(cost);return false;}
        var subclass=GetComponent<SubclassCombatState>();autoCooldownRemaining[slot]=subclass!=null&&subclass.RollCooldownBypass()?0f:EffectiveCooldown(skill);CooldownsChanged?.Invoke();return true;
    }
    public void TickAutoCooldowns(float deltaTime)=>TickAutoCooldowns(deltaTime,skill=>BattleManager.Instance!=null&&BattleManager.Instance.CanCastPlayerSkill&&BattleManager.Instance.TryCastPlayerSkill(skill));
    public void TickAutoCooldowns(float deltaTime,System.Func<PlayerSkillDefinition,bool> tryCast)
    {
        if(deltaTime<=0f||weaponSkills.Count==0||tryCast==null)return;bool changed=false;
        for(int i=0;i<weaponSkills.Count&&i<2;i++)
        {
            var skill=weaponSkills[i];if(skill==null||skill.castMode==PlayerSkillCastMode.QueuedAttackReplacement)continue;
            if(autoCooldownRemaining[i]>0f){autoCooldownRemaining[i]=Mathf.Max(0f,autoCooldownRemaining[i]-deltaTime);changed=true;}
            if(skill.castMode==PlayerSkillCastMode.ImmediateCooldown)continue;
            if(autoCooldownRemaining[i]>0f)continue;
            float cost=ManaCost(skill);if(!Mana.TrySpend(cost))continue;
            if(tryCast(skill)){var subclass=GetComponent<SubclassCombatState>();autoCooldownRemaining[i]=subclass!=null&&subclass.RollCooldownBypass()?0f:EffectiveCooldown(skill);changed=true;}else Mana.Restore(cost);
        }
        if(changed)CooldownsChanged?.Invoke();
    }
    static bool ContainsProductionSkills(List<PlayerSkillDefinition> source)
    {foreach(var skill in source)if(skill!=null&&skill.id==PlayerSkillId.SwordRapidFlurry)return true;return false;}
#if UNITY_EDITOR
    public void ConfigureDeveloperAutoSkills(PlayerSkillDefinition first,PlayerSkillDefinition second)
    {
        weaponSkills.Clear();if(first!=null)weaponSkills.Add(first);if(second!=null)weaponSkills.Add(second);
        for(int i=0;i<autoCooldownRemaining.Length;i++)autoCooldownRemaining[i]=i<weaponSkills.Count&&weaponSkills[i].castMode==PlayerSkillCastMode.AutoCooldown?EffectiveCooldown(weaponSkills[i]):0f;
        WeaponSkillsChanged?.Invoke();CooldownsChanged?.Invoke();
    }
#endif
}
