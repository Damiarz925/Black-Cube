// Developer map: Runtime active-skill picker, right-click forfeit interaction, mana readout and cast button.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerSkillMenuUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    private PaperBattleHUD hud;
    private PlayerSkillController controller;
    private GameObject panel;
    private Button castButton;
    private TMP_Text castLabel;
    private Button[] skillButtons;
    private TMP_Text[] skillLabels;

    private static readonly Color Panel = new Color(.035f, .043f, .055f, 1f);
    private static readonly Color Card = new Color(.105f, .13f, .16f, 1f);

    private void Start()
    {
        hud = GetComponent<PaperBattleHUD>();
        if (hud == null || hud.player == null) return;
        controller = hud.player.GetComponent<PlayerSkillController>();
        if (controller == null) controller = hud.player.gameObject.AddComponent<PlayerSkillController>();
        Build();
        controller.SelectionChanged += Refresh;
        controller.QueueChanged += Refresh;
        controller.Mana.ManaChanged += Refresh;
        Refresh();
    }

    private void Build()
    {
        Transform canvas = hud.GetComponentInParent<Canvas>().transform;
        castButton = Button(canvas, "Cast Active Skill", new Vector2(.42f, .012f), new Vector2(.60f, .09f), out castLabel);
        castButton.onClick.AddListener(() => { controller.TryQueueSelected(); Refresh(); });

        panel = Box(canvas, "Active Skill Menu", Vector2.zero, Vector2.one, Panel);
        var panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 110;
        panel.AddComponent<GraphicRaycaster>();

        var title = Text(panel.transform, "Title", new Vector2(.04f, .88f), new Vector2(.7f, .97f), 30);
        title.text = "WANDERER  /  ACTIVE SKILL";
        var instructions = Text(panel.transform, "Instructions", new Vector2(.04f, .79f), new Vector2(.92f, .87f), 15);
        instructions.text = "Left-click a skill to equip it. Right-click the equipped skill to forfeit it before choosing another.";
        var close = Button(panel.transform, "Close", new Vector2(.82f, .895f), new Vector2(.97f, .965f), out var closeLabel);
        closeLabel.text = "RETURN TO BATTLE";
        close.onClick.AddListener(Close);

        int count = controller.Skills.Count;
        skillButtons = new Button[count];
        skillLabels = new TMP_Text[count];
        for (int i = 0; i < count; i++)
        {
            int column = i % 2;
            int row = i / 2;
            float left = .07f + column * .46f;
            float top = .76f - row * .17f;
            skillButtons[i] = Button(panel.transform, "Skill " + i,
                new Vector2(left, top - .145f), new Vector2(left + .4f, top), out skillLabels[i]);
            int index = i;
            skillButtons[i].onClick.AddListener(() => { controller.TrySelect(controller.Skills[index]); Refresh(); });
            var rightClick = skillButtons[i].gameObject.AddComponent<SkillRightClickHandler>();
            rightClick.Clicked = () => { controller.TryForfeit(controller.Skills[index]); Refresh(); };
        }
        var footer = Text(panel.transform, "Footer", new Vector2(.07f, .035f), new Vector2(.93f, .12f), 15);
        footer.alignment = TextAlignmentOptions.Center;
        footer.text = "Skill values are provisional. A queued skill replaces the next scheduled basic attack.";
        panel.SetActive(false);
        IsOpen = false;
    }

    private void Update()
    {
        if (controller == null) return;
        if (hud.player != null && hud.player.CurrentLife <= 0f) Close();
        Refresh();
    }

    private void Refresh()
    {
        if (controller == null || castButton == null) return;
        var selected = controller.SelectedSkill;
        castButton.gameObject.SetActive(selected != null);
        if (selected != null)
        {
            float cost = controller.ManaCost(selected);
            castLabel.text = controller.QueuedSkill == selected
                ? $"QUEUED  {selected.displayName.ToUpperInvariant()}  /  NEXT ATTACK"
                : $"QUEUE  {selected.displayName.ToUpperInvariant()}  /  {cost:0} MANA";
            castButton.interactable = (controller.QueuedSkill == selected || controller.Mana.CanSpend(cost))
                && BattleManager.Instance != null && BattleManager.Instance.CanCastPlayerSkill;
            CorruptionUIButtonSkin.Ensure(castButton)?.SetSelected(controller.QueuedSkill == selected);
        }
        if (!IsOpen || skillButtons == null) return;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var skill = controller.Skills[i];
            bool equipped = selected == skill;
            string state = equipped ? "EQUIPPED  /  RIGHT-CLICK TO FORFEIT"
                : selected == null ? "LEFT-CLICK TO EQUIP" : "FORFEIT CURRENT SKILL FIRST";
            int level = controller.EffectiveSkillLevel(skill);
            float factor = PlayerSkillController.SkillDamageLevelFactor(level);
            skillLabels[i].text = $"{skill.displayName.ToUpperInvariant()}  /  LEVEL {level}  /  {factor:0.00}x DAMAGE  /  {controller.ManaCost(skill):0} MANA\n" +
                                  $"<size=14>{skill.description}</size>\n<size=11>{state}</size>";
            CorruptionUIButtonSkin.Ensure(skillButtons[i])?.SetSelected(equipped);
        }
    }

    public void Toggle()
    {
        if (panel == null || hud.player == null || hud.player.CurrentLife <= 0f) return;
        if (IsOpen) { Close(); return; }
        hud.inventoryPanel.SetActive(false);
        hud.statsPanel.SetActive(false);
        hud.CloseEnemyInspection();
        GetComponent<SkillTreeUI>()?.Close();
        IsOpen = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        IsOpen = false;
        if (controller != null)
        {
            controller.SelectionChanged -= Refresh;
            controller.QueueChanged -= Refresh;
            controller.Mana.ManaChanged -= Refresh;
        }
        if (panel != null) Destroy(panel);
    }

    private static GameObject Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TMP_Text Text(Transform parent, string name, Vector2 min, Vector2 max, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size; text.color = new Color(.92f, .93f, .95f); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private static Button Button(Transform parent, string name, Vector2 min, Vector2 max, out TMP_Text label)
    {
        var go = Box(parent, name, min, max, Card);
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        label = Text(go.transform, "Label", Vector2.zero, Vector2.one, 16);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }
}

public sealed class SkillRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    public System.Action Clicked;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) Clicked?.Invoke();
    }
}
