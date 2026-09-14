// Developer map: Runtime inventory mod-highlight menu. Simple mode selects broad
// categories; Advanced selects exact rollable affixes. Mode changes are exclusive
// and optionally confirmed with a persistent do-not-show-again preference.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryModHighlightUI : MonoBehaviour
{
    private const string SkipModeWarningKey = "BlackCube.ModFilter.SkipModeWarning";
    private readonly Dictionary<ModFilterCategory, Button> simpleButtons = new();
    private readonly Dictionary<StatTypes, Button> advancedButtons = new();
    private GameObject panel, simpleRoot, advancedRoot, confirmation;
    private Button openButton, simpleModeButton, advancedModeButton, warningCheckbox;
    private TMP_Text requiredLabel, summaryLabel, warningText, warningCheckboxLabel;
    private ModFilterMode pendingMode;
    private bool doNotShowAgain;

    private InventoryModFilter Filter => Inventory.Instance != null ? Inventory.Instance.ModHighlightFilter : null;

    private void Start()
    {
        openButton = Button(transform, "MOD HIGHLIGHT", new Vector2(.77f,.86f), new Vector2(.925f,.905f), TogglePanel, 10);
        BakedInventoryButton.Configure(openButton,InventoryArtLayout.HighlightButton);

        panel = Box(transform, "Mod highlight filter", new Color(.035f,.048f,.06f,1f));
        Place((RectTransform)panel.transform, new Vector2(.025f,.10f), new Vector2(.975f,.82f));
        Label(panel.transform, "MOD HIGHLIGHT / EXISTING INVENTORY", new Vector2(.025f,.935f), new Vector2(.975f,.99f), 16);

        simpleModeButton = Button(panel.transform, "SIMPLE", new Vector2(.025f,.865f), new Vector2(.495f,.93f), () => RequestMode(ModFilterMode.Simple), 12);
        advancedModeButton = Button(panel.transform, "ADVANCED", new Vector2(.505f,.865f), new Vector2(.975f,.93f), () => RequestMode(ModFilterMode.Advanced), 12);

        Button(panel.transform, "−", new Vector2(.025f,.785f), new Vector2(.15f,.855f), () => Filter?.SetRequiredMatches((Filter?.RequiredMatches ?? 1)-1), 18);
        requiredLabel = Label(panel.transform, "", new Vector2(.16f,.785f), new Vector2(.84f,.855f), 12);
        Button(panel.transform, "+", new Vector2(.85f,.785f), new Vector2(.975f,.855f), () => Filter?.SetRequiredMatches((Filter?.RequiredMatches ?? 1)+1), 18);

        simpleRoot = Box(panel.transform, "Simple categories", Color.clear);
        Place((RectTransform)simpleRoot.transform, new Vector2(.02f,.10f), new Vector2(.98f,.77f));
        BuildSimpleButtons();
        advancedRoot = BuildAdvancedList();

        Button(panel.transform, "CLEAR CURRENT MODE", new Vector2(.025f,.015f), new Vector2(.48f,.085f), () => Filter?.ClearCurrentMode(), 11);
        summaryLabel = Label(panel.transform, "", new Vector2(.49f,.015f), new Vector2(.975f,.085f), 10);

        BuildConfirmation();
        if (Filter != null) Filter.Changed += Refresh;
        Refresh();
        panel.SetActive(false);
    }

    private void BuildSimpleButtons()
    {
        const int columns = 4;
        var entries = InventoryModFilter.SimpleCategories;
        int rows = Mathf.CeilToInt(entries.Length / (float)columns);
        for (int i = 0; i < entries.Length; i++)
        {
            int column = i % columns;
            int row = i / columns;
            float gap = .012f;
            float width = (1f - gap * (columns + 1)) / columns;
            float height = (1f - gap * (rows + 1)) / rows;
            float x0 = gap + column * (width + gap);
            float y1 = 1f - gap - row * (height + gap);
            var entry = entries[i];
            Button button = Button(simpleRoot.transform, entry.Label,
                new Vector2(x0, y1-height), new Vector2(x0+width, y1), () => Filter?.Toggle(entry.Category), 10);
            button.GetComponentInChildren<TMP_Text>().enableAutoSizing = true;
            simpleButtons[entry.Category] = button;
        }
    }

    private GameObject BuildAdvancedList()
    {
        GameObject viewport = Box(panel.transform, "Advanced exact mods", new Color(.02f,.028f,.035f,.8f));
        Place((RectTransform)viewport.transform, new Vector2(.025f,.10f), new Vector2(.975f,.77f));
        viewport.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 34f;

        var contentObject = new GameObject("Exact mod list", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform content = (RectTransform)contentObject.transform;
        content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1);
        content.offsetMin = new Vector2(7,0); content.offsetMax = new Vector2(-7,0); content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4,4,5,5); layout.spacing = 4; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = content;

        string lastGroup = null;
        foreach (StatTypes stat in InventoryModFilter.SelectableStats)
        {
            string group = InventoryModFilter.AdvancedGroup(stat);
            if (group != lastGroup)
            {
                TMP_Text header = ListLabel(content, group.ToUpperInvariant(), 12, 27);
                header.color = new Color(.85f,.68f,.35f);
                lastGroup = group;
            }
            Button button = ListButton(content, StatDisplayFormatting.ToFriendlyName(stat), () => Filter?.Toggle(stat));
            advancedButtons[stat] = button;
        }
        viewport.SetActive(false);
        return viewport;
    }

    private void BuildConfirmation()
    {
        confirmation = Box(panel.transform, "Confirm filter mode change", new Color(.01f,.014f,.02f,.82f));
        Place((RectTransform)confirmation.transform, Vector2.zero, Vector2.one);
        GameObject dialog = Box(confirmation.transform, "Confirmation dialog", new Color(.018f,.023f,.03f,.995f));
        Place((RectTransform)dialog.transform, new Vector2(.07f,.22f), new Vector2(.93f,.78f));
        var outline = dialog.AddComponent<Outline>();
        outline.effectColor = new Color(1f,.5f,.12f,.9f); outline.effectDistance = new Vector2(2,-2);
        warningText = Label(dialog.transform, "", new Vector2(.06f,.52f), new Vector2(.94f,.92f), 15);
        warningText.textWrappingMode = TextWrappingModes.Normal;
        warningCheckbox = Button(dialog.transform, "", new Vector2(.08f,.36f), new Vector2(.92f,.51f), ToggleWarningPreference, 12);
        warningCheckboxLabel = warningCheckbox.GetComponentInChildren<TMP_Text>();
        Button(dialog.transform, "NO", new Vector2(.08f,.10f), new Vector2(.46f,.29f), CancelModeChange, 14);
        Button(dialog.transform, "YES", new Vector2(.54f,.10f), new Vector2(.92f,.29f), ConfirmModeChange, 14);
        confirmation.SetActive(false);
    }

    private void TogglePanel()
    {
        GetComponent<InventoryUI>().Tooltip?.Hide();
        bool open = !panel.activeSelf;
        GetComponent<InventoryFilterUI>()?.Close();
        panel.SetActive(open);
        if (!open) confirmation.SetActive(false);
        Refresh();
    }

    public void Close()
    {
        if (confirmation != null) confirmation.SetActive(false);
        if (panel != null) panel.SetActive(false);
        BakedInventoryButton.SetEngaged(openButton,false);
    }

    private void RequestMode(ModFilterMode mode)
    {
        if (Filter == null || Filter.Mode == mode) return;
        if (Filter.HasSelection && PlayerPrefs.GetInt(SkipModeWarningKey, 0) == 0)
        {
            pendingMode = mode;
            doNotShowAgain = false;
            warningText.text = $"Switch to {mode.ToString().ToUpperInvariant()} filtering?\n\nYour current {Filter.Mode.ToString().ToUpperInvariant()} mod selections will be erased.";
            confirmation.SetActive(true);
            confirmation.transform.SetAsLastSibling();
            RefreshWarningCheckbox();
            return;
        }
        Filter.SetMode(mode);
    }

    private void ToggleWarningPreference()
    {
        doNotShowAgain = !doNotShowAgain;
        RefreshWarningCheckbox();
    }

    private void RefreshWarningCheckbox()
    {
        warningCheckboxLabel.text = $"[{(doNotShowAgain ? "X" : " ")}]  DO NOT SHOW THIS AGAIN";
        CorruptionUIButtonSkin.Ensure(warningCheckbox)?.SetSelected(doNotShowAgain);
    }

    private void ConfirmModeChange()
    {
        if (doNotShowAgain)
        {
            PlayerPrefs.SetInt(SkipModeWarningKey, 1);
            PlayerPrefs.Save();
        }
        confirmation.SetActive(false);
        Filter?.SetMode(pendingMode);
    }

    private void CancelModeChange()
    {
        doNotShowAgain = false;
        confirmation.SetActive(false);
    }

    private void Refresh()
    {
        if (Filter == null || panel == null) return;
        bool simple = Filter.Mode == ModFilterMode.Simple;
        simpleRoot.SetActive(simple);
        advancedRoot.SetActive(!simple);
        requiredLabel.text = $"HIGHLIGHT WHEN AT LEAST {Filter.RequiredMatches} SELECTED MOD{(Filter.RequiredMatches == 1 ? "" : "S")} MATCH";
        int selected = simple
            ? InventoryModFilter.SimpleCategories.Count(entry => Filter.IsSelected(entry.Category))
            : Filter.AdvancedSelection.Count;
        summaryLabel.text = selected == 0 ? "NO MODS SELECTED" : $"{selected} SELECTED / NEED {Filter.RequiredMatches}";
        BakedInventoryButton.SetEngaged(openButton,panel.activeSelf);
        CorruptionUIButtonSkin.Ensure(simpleModeButton)?.SetSelected(simple);
        CorruptionUIButtonSkin.Ensure(advancedModeButton)?.SetSelected(!simple);
        foreach (var pair in simpleButtons)
            CorruptionUIButtonSkin.Ensure(pair.Value)?.SetSelected(Filter.IsSelected(pair.Key));
        foreach (var pair in advancedButtons)
            CorruptionUIButtonSkin.Ensure(pair.Value)?.SetSelected(Filter.IsSelected(pair.Key));
        GetComponent<InventoryUI>()?.RefreshModHighlights();
    }

    private void OnDisable()
    {
        if (confirmation != null) confirmation.SetActive(false);
        Close();
    }

    private void OnDestroy()
    {
        if (Filter != null) Filter.Changed -= Refresh;
    }

    private static GameObject Box(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.color = color;
        return go;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; }

    private static TMP_Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text label = go.GetComponent<TextMeshProUGUI>();
        label.text=text; label.fontSize=size; label.color=new Color(.92f,.93f,.95f);
        label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false;
        Place(label.rectTransform,min,max);
        return label;
    }

    private static TMP_Text ListLabel(Transform parent, string text, int size, float height)
    {
        TMP_Text label = Label(parent,text,Vector2.zero,Vector2.one,size);
        var element = label.gameObject.AddComponent<LayoutElement>(); element.preferredHeight=height;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private static Button Button(Transform parent, string label, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction action, int size)
    {
        GameObject go=Box(parent,label,new Color(.14f,.19f,.22f)); Place((RectTransform)go.transform,min,max);
        Button button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); button.onClick.AddListener(action);
        Label(go.transform,label,Vector2.zero,Vector2.one,size);
        CorruptionUIButtonSkin.Ensure(button);
        return button;
    }

    private static Button ListButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject go=Box(parent,label,new Color(.14f,.19f,.22f));
        var element=go.AddComponent<LayoutElement>(); element.preferredHeight=34;
        Button button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); button.onClick.AddListener(action);
        TMP_Text text=Label(go.transform,label,new Vector2(.025f,0),new Vector2(.975f,1),11);
        text.alignment=TextAlignmentOptions.MidlineLeft;
        CorruptionUIButtonSkin.Ensure(button);
        return button;
    }
}
