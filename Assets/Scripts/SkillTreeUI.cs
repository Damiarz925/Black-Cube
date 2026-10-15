// Developer map: Builds the six-node skill panel over PaperBattleHUD and calls PlayerProgression.TrySpend. IsOpen freezes combat and sprite clocks without setting global time scale.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-local, expandable skill canvas. Combat pauses while allocating points.</summary>
public sealed class SkillTreeUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    PaperBattleHUD hud;
    GameObject panel;
    TMP_Text points, xp, openLabel;
    readonly Button[] nodes = new Button[6];
    readonly TMP_Text[] labels = new TMP_Text[6];
    PlayerProgression progression;
    public GameObject Panel => panel;
    public Button NodeButton(int index) => nodes[index];
    void Start()
    {
        hud = GetComponent<PaperBattleHUD>();
        var parent = hud.GetComponentInParent<Canvas>().transform;
        var open = Button(transform, "Skills", new Vector2(.62f,.25f), new Vector2(.735f,.76f), out openLabel);
        open.onClick.AddListener(Toggle);
        xp = Text(transform, "Progression", new Vector2(.028f,-.29f), new Vector2(.29f,0), 12);
        xp.color = new Color(.95f,.8f,.45f);
        panel = Box(parent, "Skill Tree", Vector2.zero, Vector2.one, new Color(.035f,.043f,.055f,1));
        // Separate canvas keeps the screen above actor badges/tooltips.
        var canvas = panel.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 100;
        panel.AddComponent<GraphicRaycaster>();
        var title = Text(panel.transform, "Title", new Vector2(.035f,.89f), new Vector2(.55f,.975f), 30);
        title.text = "WANDERER  /  PASSIVE SKILLS";
        points = Text(panel.transform, "Available points", new Vector2(.035f,.82f), new Vector2(.73f,.9f), 17);
        var close = Button(panel.transform, "Close", new Vector2(.83f,.895f), new Vector2(.97f,.965f), out var closeText);
        closeText.text = "RETURN TO BATTLE"; close.onClick.AddListener(Close);
        var viewport = Box(panel.transform, "Tree viewport", new Vector2(.025f,.10f), new Vector2(.975f,.81f), new Color(.06f,.075f,.09f));
        viewport.AddComponent<RectMask2D>();
        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45;
        var content = new GameObject("Expandable tree", typeof(RectTransform)); content.transform.SetParent(viewport.transform,false);
        var rect = (RectTransform)content.transform; rect.sizeDelta = new Vector2(2400,1400);
        scroll.viewport = (RectTransform)viewport.transform; scroll.content = rect;
        // Three small branches, with generous space beyond them for future nodes.
        Vector2 root = new Vector2(0,220);
        for (int i=0;i<6;i++)
        {
            int branch = i % 3;
            Vector2 pos = new Vector2((branch-1)*390, i<3 ? 50 : -160);
            Line(content.transform, i<3 ? root : new Vector2((branch-1)*390,50), pos);
            nodes[i] = Button(content.transform, "Node " + i, Vector2.one*.5f, Vector2.one*.5f, out labels[i]);
            var nr = (RectTransform)nodes[i].transform; nr.sizeDelta = new Vector2(310,135); nr.anchoredPosition = pos;
            int node = i; nodes[i].onClick.AddListener(() => { if(progression != null) progression.TrySpend(node); Refresh(); });
        }
        var origin = Text(content.transform, "Origin", Vector2.one*.5f, Vector2.one*.5f, 22);
        origin.rectTransform.sizeDelta = new Vector2(300,90); origin.rectTransform.anchoredPosition = root;
        origin.alignment = TextAlignmentOptions.Center; origin.text = "WANDERER\n<size=14>START</size>";
        var hint = Text(panel.transform, "Instructions", new Vector2(.035f,.02f), new Vector2(.96f,.09f), 14);
        hint.text = "Each rank costs 1 point. Branch upgrades require the first node. Drag to explore. Combat pauses here.\nMaximum HP nodes increase capacity; your next level-up fills it. More branches will follow.";
        panel.SetActive(false); IsOpen = false;
        Bind();
    }
    void Update() { Bind(); Refresh(); if (hud.player != null && hud.player.CurrentLife <= 0) Close(); }
    void Bind()
    {
        if (progression != null || GameManager.Instance == null) return;
        progression = GameManager.Instance.GetComponent<PlayerProgression>();
    }
    void Refresh()
    {
        if (progression == null || points == null) return;
        string value = progression.AtCap ? "MAX LEVEL" : $"{progression.Experience:0} / {progression.RequiredXp:0} XP";
        xp.text = $"LEVEL {progression.Level}   /   {value}";
        openLabel.text = $"SKILLS ({progression.AvailablePoints})";
        points.text = $"{progression.AvailablePoints} POINTS AVAILABLE     /     LEVEL {progression.Level}     /     {value}";
        if (!IsOpen) return;
        for (int i=0;i<6;i++)
        {
            int rank = progression.Rank(i);
            string state = rank == 5 ? "COMPLETE" : i >= 3 && progression.Rank(i-3) == 0 ? "REQUIRES " + PlayerProgression.NodeNames[i-3].ToUpperInvariant() : "1 POINT / RANK";
            labels[i].text = $"{PlayerProgression.NodeNames[i].ToUpperInvariant()}  {rank}/5\n<size=15>{PlayerProgression.NodeBonuses[i]} per rank</size>\n<size=12>{state}</size>";
            nodes[i].interactable = progression.CanSpend(i);
            nodes[i].GetComponent<Image>().color = rank > 0 ? new Color(.28f,.24f,.13f) : new Color(.12f,.16f,.20f);
        }
    }
    public void Toggle()
    {
        if (panel == null || hud.player == null || hud.player.CurrentLife <= 0) return;
        if (IsOpen) { Close(); return; }
        hud.inventoryPanel.SetActive(false); hud.statsPanel.SetActive(false);
        IsOpen = true; panel.SetActive(true); panel.transform.SetAsLastSibling(); Refresh();
    }
    public void Close() { IsOpen = false; if (panel != null) panel.SetActive(false); }
    void OnDestroy() { IsOpen = false; if (panel != null) Destroy(panel); }
    static GameObject Box(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var r = (RectTransform)go.transform; r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        go.GetComponent<Image>().color=color; return go;
    }
    static TMP_Text Text(Transform parent,string name,Vector2 min,Vector2 max,int size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        var text=go.GetComponent<TextMeshProUGUI>();text.fontSize=size;text.color=new Color(.92f,.93f,.95f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;
        return text;
    }
    static Button Button(Transform parent,string name,Vector2 min,Vector2 max,out TMP_Text label)
    {
        var go=Box(parent,name,min,max,new Color(.12f,.15f,.18f));var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
        label=Text(go.transform,"Label",Vector2.zero,Vector2.one,16);label.alignment=TextAlignmentOptions.Center;
        return button;
    }
    static void Line(Transform parent,Vector2 from,Vector2 to)
    {
        var go=Box(parent,"Connection",Vector2.one*.5f,Vector2.one*.5f,new Color(.45f,.38f,.23f));
        var r=(RectTransform)go.transform;r.anchoredPosition=(from+to)/2;r.sizeDelta=new Vector2(Vector2.Distance(from,to),3);
        r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);go.GetComponent<Image>().raycastTarget=false;
        go.transform.SetAsFirstSibling();
    }
}
