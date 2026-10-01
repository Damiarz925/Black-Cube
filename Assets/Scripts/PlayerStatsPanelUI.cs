// Developer map: Rebuilds categorized stat rows on StatsChanged/AttackChanged while retaining collapsed sections. Attack previews use the noncritical context so inspecting stats does not consume random rolls.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsPanelUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private StatsComponent playerStats;

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private StatHeaderUI headerPrefab;
    [SerializeField] private StatRowUI rowPrefab;

    [Header("Behavior")]
    [Tooltip("If > 0, refreshes repeatedly. If 0, only refreshes on enable / manual calls.")]
    [SerializeField] private float autoRefreshInterval = 0f;

    private float _timer;
    readonly List<StatRowUI> rowPool=new();
    readonly List<StatHeaderUI> headerPool=new();
    int rowCursor,headerCursor,siblingCursor;
    bool initializedAdvancedCollapse;

    private readonly List<GameObject> _spawned = new();
    private StatsComponent subscribedStats;
    private PlayerController subscribedPlayer;
    private readonly HashSet<string> collapsed = new();
    private readonly Dictionary<string, Button> sectionButtons = new();
    public bool IsSectionExpanded(string section) => !collapsed.Contains(section);
    public Button SectionButton(string section) => sectionButtons.TryGetValue(section, out var button) ? button : null;

    private void OnEnable()
    {
        Bind();
        Refresh();
        _timer = 0f;
    }

    private void Update()
    {
        if (autoRefreshInterval <= 0f) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autoRefreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public void SetTarget(StatsComponent target)
    {
        Unbind();
        playerStats = target;
        if (isActiveAndEnabled) Bind();
        Refresh();
    }

    private void Bind()
    {
        Unbind();
        subscribedStats = playerStats;
        subscribedPlayer = playerStats != null ? playerStats.GetComponent<PlayerController>() : null;
        if (subscribedStats != null) subscribedStats.StatsChanged += Refresh;
        if (subscribedPlayer != null) subscribedPlayer.AttackChanged += Refresh;
    }
    private void Unbind()
    {
        if (subscribedStats != null) subscribedStats.StatsChanged -= Refresh;
        if (subscribedPlayer != null) subscribedPlayer.AttackChanged -= Refresh;
        subscribedStats = null; subscribedPlayer = null;
    }
    private void OnDisable() { Unbind(); }

    public void Refresh()
    {
        if (contentRoot == null || headerPrefab == null || rowPrefab == null)
            return;

        BeginRefresh();
        try
        {
        if (playerStats == null)
        {
            AddHeader("Stats");
            var empty = RentRow();
            empty.Set("No current target", "");
            _spawned.Add(empty.gameObject);
            return;
        }

        var player = playerStats.GetComponent<PlayerController>();
        if(player!=null){BuildCompactPlayerStats(player);return;}
        if (player != null)
        {
            var header = RentHeader();
            header.SetText("Basic Attack / Before Defenses"); _spawned.Add(header.gameObject);
            var lowContext = player.BuildNonCriticalAttackContextAtRangeEnd(false);
            var highContext = player.BuildNonCriticalAttackContextAtRangeEnd(true);
            for (int i = 0; i < lowContext.Hits.Count; i++)
            {
                var hit = lowContext.Hits[i];
                if (hit.Amount <= 0f) continue;
                var damage = RentRow();
                float high = i < highContext.Hits.Count ? highContext.Hits[i].Amount : hit.Amount;
                damage.Set($"{ItemTooltipUI.ElementName(hit.Element)} / Hit (Noncritical)",
                    $"{hit.Amount:0.##}-{high:0.##} (Average {(hit.Amount+high)*.5f:0.##})");
                _spawned.Add(damage.gameObject);
            }
            var crit = RentRow();
            crit.Set("Critical Chance (Final)", (player.GetFinalCritChance() * 100f).ToString("0.##") + "%");
            _spawned.Add(crit.gameObject);
            var speed = RentRow();
            speed.Set("Attacks / Second (Final)", player.GetFinalAttackSpeed().ToString("0.##"));
            _spawned.Add(speed.gameObject);
            if (player.EquippedWeaponBaseDamage != 0)
            {
                var weapon = RentRow();
                player.EquippedWeapon.GetEffectiveBaseDamageRange(out float low, out float high);
                weapon.Set($"Weapon Base {ItemTooltipUI.ElementName(player.EquippedWeaponElement)}",
                    $"{low:0.##}-{high:0.##} (Average {(low+high)*.5f:0.##})");
                _spawned.Add(weapon.gameObject);
                AddInspectionRow("Weapon Type",WeaponTypeCatalog.Get(player.EquippedWeapon.WeaponTypeId).DisplayName);
                AddInspectionRow("Damage Scaling",WeaponAttributeScalingProfile.ScalingAttributes(player.EquippedWeapon.WeaponTypeId));
                AddInspectionRow("Attribute Damage Bonus",(player.WeaponAttributeDamageBonus*100f).ToString("0.##")+"%");
                AddInspectionRow("Local Weapon DPS",player.EquippedWeapon.GetAverageWeaponDps().ToString("0.##"));
            }
            AddInspectionRow("Player Level",player.CurrentPlayerLevel.ToString());
            AddInspectionRow("Level Damage Bonus",(player.LevelDamageBonus*100f).ToString("0.##")+"%");
            AddInspectionRow("Strength",DerivedStatCalculator.Strength(playerStats).ToString("0.##"));
            AddInspectionRow("Dexterity",DerivedStatCalculator.Dexterity(playerStats).ToString("0.##"));
            AddInspectionRow("Intelligence",DerivedStatCalculator.Intelligence(playerStats).ToString("0.##"));
            if (StatDisplayFormatting.ShouldDisplay(playerStats, StatTypes.UnarmedDamage))
            {
                var unarmed = RentRow();
                unarmed.Set("Unarmed Damage", playerStats.GetStat(StatTypes.UnarmedDamage).ToString("0.##"));
                _spawned.Add(unarmed.gameObject);
            }
        }
        else
        {
            var enemy = playerStats.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                AddHeader("Basic Attack / Before Defenses");
                var lowContext = enemy.BuildNonCriticalAttackContextAtRangeEnd(false);
                var highContext = enemy.BuildNonCriticalAttackContextAtRangeEnd(true);
                for (int i = 0; i < lowContext.Hits.Count; i++)
                {
                    var hit = lowContext.Hits[i];
                    if (hit.Amount <= 0f) continue;
                    var damage = RentRow();
                    float high = i < highContext.Hits.Count ? highContext.Hits[i].Amount : hit.Amount;
                    damage.Set($"{ItemTooltipUI.ElementName(hit.Element)} / Hit (Noncritical)",
                        $"{hit.Amount:0.##}-{high:0.##} (Average {(hit.Amount+high)*.5f:0.##})");
                    _spawned.Add(damage.gameObject);
                }
                var crit = RentRow();
                crit.Set("Critical Chance (Final)", (enemy.GetFinalCritChance() * 100f).ToString("0.##") + "%");
                _spawned.Add(crit.gameObject);
                var speed = RentRow();
                speed.Set("Attacks / Second (Final)", enemy.GetFinalAttackSpeed().ToString("0.##"));
                _spawned.Add(speed.gameObject);
                if (enemy.EquippedWeaponBaseDamage != 0f)
                {
                    var weapon = RentRow();
                    enemy.EquippedWeapon.GetEffectiveBaseDamageRange(out float low, out float high);
                    weapon.Set($"Weapon Base {ItemTooltipUI.ElementName(enemy.WeaponMainElement)}",
                        $"{low:0.##}-{high:0.##} (Average {(low+high)*.5f:0.##})");
                    _spawned.Add(weapon.gameObject);
                }
            }
        }

        // Only display stats that the player currently has => tracked keys
        var tracked = playerStats.GetTrackedStats().OrderBy(t => (int)t).ToList();

        // Only display stats with non-zero values
        tracked = tracked.Where(t => StatDisplayFormatting.ShouldDisplay(playerStats, t)).ToList();
        if (player != null) tracked.RemoveAll(t => t == StatTypes.UnarmedDamage || t == StatTypes.WeaponBaseDmg);

        if (tracked.Count == 0)
        {
            // Show a single header that says "No Stats"
            var h = RentHeader();
            h.SetText("Stats");
            _spawned.Add(h.gameObject);

            var r = RentRow();
            r.Set("No active stats", "");
            _spawned.Add(r.gameObject);
            return;
        }

        foreach (string section in new[] { "Damage", "Critical & Speed", "Status Effects", "Defenses", "Resources", "Attributes", "Other" })
        {
            var values = tracked.Where(t => Section(t) == section).ToList();
            if (values.Count == 0) continue;
            var h = AddHeader((IsSectionExpanded(section) ? "[-] " : "[+] ") + section);
            var background = h.GetComponent<Image>();
            if (background == null) background = h.gameObject.AddComponent<Image>();
            background.color = new Color(.10f,.12f,.15f,1); background.raycastTarget = true;
            foreach(var text in h.GetComponentsInChildren<TMP_Text>()) text.raycastTarget = false;
            var button = h.GetComponent<Button>() ?? h.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            CorruptionUIButtonSkin.Ensure(button)?.SetSelected(IsSectionExpanded(section));
            sectionButtons[section] = button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!collapsed.Add(section)) collapsed.Remove(section);
                Refresh();
            });
            if (!IsSectionExpanded(section)) continue;
            if (section == "Status Effects")
            {
                foreach (string sub in new[] { "Application Chances", "Damage / Increases & Multipliers", "Effect Strength", "Duration & Tick Rate", "Penetration", "Resistances", "Other Status Modifiers" })
                {
                    var subset = values.Where(t => StatusSubsection(t) == sub).ToList();
                    if (subset.Count == 0) continue;
                    AddHeader(sub);
                    foreach (var type in subset) AddStat(type);
                }
            }
            else foreach (var type in values) AddStat(type);
        }

        }
        finally { EndRefresh(); }
    }

    private StatHeaderUI AddHeader(string title)
    { var h = RentHeader(); h.SetText(title); _spawned.Add(h.gameObject); return h; }
    void BuildCompactPlayerStats(PlayerController player)
    {
        var hit=CharacterDamageEstimate.SearchHit(player.BuildNonCriticalAttackContext(),playerStats);
        var weapon=player.EquippedWeapon;
        AddHeader("OFFENSE");AddInspectionRow("Weapon Type",weapon!=null?WeaponTypeCatalog.Get(weapon.WeaponTypeId).DisplayName:"Unarmed");
        AddInspectionRow(weapon!=null?"Average Hit":"Unarmed Damage / Hit",hit.Hits.Sum(h=>h.Amount).ToString("0.#"));
        AddInspectionRow("Basic DPS (before defenses)",CharacterDamageEstimate.Calculate(player,false).ToString("0.#"));
        AddInspectionRow("Attacks / Second",player.GetFinalAttackSpeed().ToString("0.##"));
        AddInspectionRow("Critical Chance",player.GetFinalCritChance().ToString("P1"));
        AddInspectionRow("Critical Multiplier",(CombatCalculator.BaseCriticalMultiplier+playerStats.GetStat(StatTypes.CritMult)).ToString("0.##")+"×");
        AddStat(StatTypes.ChanceToHitTwice);
        AddHeader("DAMAGE TYPES / AVERAGE HIT");
        foreach(var element in new[]{Element.Phys,Element.Fire,Element.Cold,Element.Light,Element.Void})
        {float amount=hit.Hits.Where(h=>h.Element==element).Sum(h=>h.Amount);if(amount>0.0001f)AddInspectionRow(ItemTooltipUI.ElementName(element),amount.ToString("0.#"));}
        AddHeader("DEFENSE");
        float manaBeforeLife=ManaBeforeLifeRules.Fraction(playerStats);
        if(manaBeforeLife>0.0001f)AddInspectionRow("Damage Taken From Mana Before Life",manaBeforeLife.ToString("P1"),
            manaBeforeLife.ToString("P1")+" of post-mitigation damage is taken from Mana before Life. Damage that cannot be absorbed because Mana is empty is taken from Life.");
        AddInspectionRow("Maximum Life",(player.GetComponent<HealthComponent>()?.MaxLife??0).ToString("0"));
        float armour=ItemArmourProfile.Final(playerStats),explicitPdr=playerStats.GetStat(StatTypes.PhysicalDamageReduction);
        if(armour>0.0001f)AddInspectionRow("Armour",armour.ToString("0"));
        float pdr=Mathf.Clamp(ItemArmourProfile.RawReduction(armour,ItemArmourProfile.ReferenceHit,explicitPdr),-.9f,.9f);
        if(pdr>0.0001f)AddInspectionRow("PDR vs L100 reference",pdr.ToString("P1"));
        AddHeader("RESISTANCES");
        foreach(var element in new[]{Element.Fire,Element.Cold,Element.Light,Element.Void})
        {
            var stat=element switch{Element.Fire=>StatTypes.FireRes,Element.Cold=>StatTypes.ColdRes,Element.Light=>StatTypes.LightRes,_=>StatTypes.VoidRes};
            float resistance=Mathf.Min(CombatCalculator.GetMaximumResistance(element,playerStats),playerStats.GetStat(stat)+(element==Element.Void?0:playerStats.GetStat(StatTypes.AllRes)));
            if(Mathf.Abs(resistance)>0.0001f)AddInspectionRow(ItemTooltipUI.ElementName(element)+" Resistance",resistance.ToString("P0"));
        }
        AddHeader("RESOURCES");AddInspectionRow("Maximum Mana",(player.GetComponent<ManaComponent>()?.MaxMana??0).ToString("0"));
        float lifeRegen=playerStats.GetStat(StatTypes.LifeRegeneration)*(player.GetComponent<PassiveKeystoneState>()?.LifeRegenerationMultiplier??1);
        if(lifeRegen>0.0001f)AddInspectionRow("Life Regeneration / sec",lifeRegen.ToString("P2")+" Max Life");
        foreach(var stat in new[]{StatTypes.ManaRegeneration,StatTypes.LifeOnHit,StatTypes.ManaOnHit,StatTypes.LifeOnKill,StatTypes.ManaOnKill})if(StatDisplayFormatting.ShouldDisplay(playerStats,stat))AddStat(stat);
        AddHeader("SHOCK");
        float increased=playerStats.GetStat(StatTypes.ShockEffect)+(playerStats.GetComponent<SubclassCombatState>()?.AuraSecondary(3,.20f)??0)
            +(playerStats.GetComponent<StatusController>()?.CombinedShockEffect>0?UniqueCatalog.Power(playerStats,UniquePower.ShockedShockEffect):0);
        float cap=ShockRules.BaseMaximumEffect+(RelicInventory.Instance?.MaximumShockEffectIncrease??0);
        AddInspectionRow("Base Shock Effect",ShockRules.BaseEffect.ToString("P0"));
        AddInspectionRow("Current Increased Shock Effect",increased.ToString("P1"));
        AddInspectionRow("Current Shock Effect",ShockRules.Effect(increased,cap).ToString("P1"));
        AddInspectionRow("Maximum Shock Effect",cap.ToString("P1"));
        AddInspectionRow("Maximum Shock Stacks",(playerStats.GetComponent<SubclassCombatState>()?.MaximumShockInstances??1).ToString());
        AddInspectionRow("Shock Duration",ShockRules.Duration(playerStats.GetStat(StatTypes.ShockDuration)).ToString("0.##")+"s");
        AddHeader("SPECIAL");foreach(var stat in new[]{StatTypes.CooldownReduction,StatTypes.SpellEchoChance,StatTypes.AuraEffect,StatTypes.ProjectileSpeed,StatTypes.ProjectileAmount,StatTypes.ProjectilePrecisionChance,StatTypes.ProjectilePrecisionMultiplier})if(StatDisplayFormatting.ShouldDisplay(playerStats,stat))AddStat(stat);
        const string details="Advanced Sources";
        if(!initializedAdvancedCollapse){collapsed.Add(details);initializedAdvancedCollapse=true;}
        var header=AddHeader((collapsed.Contains(details)?"[+] ":"[-] ")+details);var image=header.GetComponent<Image>()??header.gameObject.AddComponent<Image>();image.raycastTarget=true;
        foreach(var text in header.GetComponentsInChildren<TMP_Text>())text.raycastTarget=false;
        var button=header.GetComponent<Button>()??header.gameObject.AddComponent<Button>();button.targetGraphic=image;sectionButtons[details]=button;
        button.onClick.RemoveAllListeners();
            button.onClick.AddListener(()=>{if(!collapsed.Add(details))collapsed.Remove(details);Refresh();});
        if(!collapsed.Contains(details))
        {
            var gear=EquipmentManager.Instance?.EquippedItems.Values;
            if(gear!=null)foreach(var item in gear)if(ItemArmourProfile.IsArmour(item.ItemType))AddInspectionRow(ItemSlotUI.DisplayType(item.ItemType),ItemArmourProfile.Diagnose(item));
            AddInspectionRow("Global Increased Armour",playerStats.GetStat(StatTypes.ArmourPercent).ToString("P1"));
            AddInspectionRow("Raw PDR vs reference",ItemArmourProfile.RawReduction(armour,ItemArmourProfile.ReferenceHit,explicitPdr).ToString("P2"));
            foreach(var stat in playerStats.GetTrackedStats().Where(s=>s!=StatTypes.AllRes&&!Gear.IsWeaponBaseStat(s)&&StatDisplayFormatting.ShouldDisplay(playerStats,s)))AddStat(stat);
        }
    }
    private void AddInspectionRow(string label,string value,string explanation=null)
    {var row=RentRow();row.Set(label,value,explanation);_spawned.Add(row.gameObject);}
    private void AddStat(StatTypes type)
    {
        var row = RentRow();
        row.Set(StatDisplayFormatting.ToFriendlyName(type), StatDisplayFormatting.FormatValue(playerStats,type));
        _spawned.Add(row.gameObject);
    }
    private static bool IsStatusStat(StatTypes type) =>
        ((int)type >= (int)StatTypes.PoisonDmg && (int)type <= (int)StatTypes.ChillDuration)
        || ((int)type >= (int)StatTypes.PoisonRes && (int)type <= (int)StatTypes.AllAilmentRes)
        || type == StatTypes.GenericDotMult || type == StatTypes.DoTMultPerIntelligence
        || type == StatTypes.Plus1Poison || type == StatTypes.Plus1Bleed || type == StatTypes.Plus1Ignite;
    private static string Section(StatTypes type)
    {
        if (IsStatusStat(type)) return "Status Effects";
        if (type == StatTypes.ChanceToBlock) return "Defenses";
        return StatCategoryMapping.GetCategory(type) switch
        {
            StatCategory.WeaponBase or StatCategory.FlatDamage or StatCategory.IncreasedDamage or StatCategory.MoreDamage or StatCategory.Penetration => "Damage",
            StatCategory.Utility => "Critical & Speed",
            StatCategory.Defenses => "Defenses",
            StatCategory.Resources => "Resources",
            StatCategory.Attributes => "Attributes",
            _ => "Other"
        };
    }
    private static string StatusSubsection(StatTypes type) => type switch
    {
        StatTypes.PoisonChance or StatTypes.BleedChance or StatTypes.IgniteChance or StatTypes.ChillChance or StatTypes.ShockChance => "Application Chances",
        StatTypes.PoisonDmg or StatTypes.IgniteDmg or StatTypes.BleedDmg or StatTypes.PoisonMult or StatTypes.IgniteMult or StatTypes.BleedMult or StatTypes.GenericDotMult or StatTypes.DoTMultPerIntelligence => "Damage / Increases & Multipliers",
        StatTypes.ShockEffect or StatTypes.ChillEffect => "Effect Strength",
        StatTypes.PoisonDuration or StatTypes.BleedDuration or StatTypes.IgniteDuration or StatTypes.ChillDuration or StatTypes.ShockDuration or StatTypes.PoisonTickRate or StatTypes.BleedTickRate or StatTypes.IgniteTickRate => "Duration & Tick Rate",
        StatTypes.PoisonPenetration or StatTypes.IgnitePenetration or StatTypes.BleedPenetration => "Penetration",
        StatTypes.PoisonRes or StatTypes.BleedRes or StatTypes.IgniteRes or StatTypes.ChillRes or StatTypes.ShockRes or StatTypes.AllAilmentRes => "Resistances",
        _ => "Other Status Modifiers"
    };

    void BeginRefresh()
    {
        sectionButtons.Clear();_spawned.Clear();rowCursor=headerCursor=siblingCursor=0;
    }
    StatRowUI RentRow()
    {
        if(rowCursor==rowPool.Count)rowPool.Add(Instantiate(rowPrefab,contentRoot));
        var row=rowPool[rowCursor++];Use(row.gameObject);return row;
    }
    StatHeaderUI RentHeader()
    {
        if(headerCursor==headerPool.Count)headerPool.Add(Instantiate(headerPrefab,contentRoot));
        var header=headerPool[headerCursor++];
        var button=header.GetComponent<Button>();if(button!=null)button.onClick.RemoveAllListeners();
        Use(header.gameObject);return header;
    }
    void Use(GameObject go)
    {
        if(!go.activeSelf)go.SetActive(true);
        if(go.transform.GetSiblingIndex()!=siblingCursor)go.transform.SetSiblingIndex(siblingCursor);
        siblingCursor++;
    }
    void EndRefresh()
    {
        for(int i=rowCursor;i<rowPool.Count;i++)if(rowPool[i].gameObject.activeSelf)rowPool[i].gameObject.SetActive(false);
        for(int i=headerCursor;i<headerPool.Count;i++)if(headerPool[i].gameObject.activeSelf)headerPool[i].gameObject.SetActive(false);
    }
}
