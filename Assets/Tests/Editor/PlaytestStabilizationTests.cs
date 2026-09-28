using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TMPro;

public sealed class PlaytestStabilizationTests
{
    static void Field(object instance,string name,object value)=>instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);
    [Test] public void OffClassPopupAfterSubclassAndKeystoneSwapAllocatesBothSidesAfterRestore()
    {
        var managerRoot=new GameObject("Popup regression identity");managerRoot.SetActive(false);
        var uiRoot=new GameObject("Popup regression UI");
        var previous=GameManager.Instance;
        var openField=typeof(SkillTreeUI).GetField("<IsOpen>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        bool wasOpen=SkillTreeUI.IsOpen;
        try
        {
            var identity=managerRoot.AddComponent<PlayerIdentityState>();identity.BeginNewGame(PlayerClassIds.Warrior);
            identity.CompleteMilestone(PlayerIdentityState.StoryCompletionMilestoneId);
            Assert.That(identity.SelectSubclass(SubclassIds.WarriorBleed),Is.True);
            var manager=managerRoot.AddComponent<GameManager>();typeof(GameManager).GetProperty("Instance").SetValue(null,manager);
            var p=managerRoot.GetComponent<PlayerProgression>();Assert.That(p.RestoreProgression(100,0,100,new int[PassiveTreeDefinition.NodeCount]),Is.True);
            ClassKeystoneProgressionTests.AllocateThirty(p,PlayerClassIds.Warrior);p.TrySpend(ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First());
            p.RefundAll();Assert.That(identity.SelectSubclass(SubclassIds.WarriorMultihit),Is.True);
            ClassKeystoneProgressionTests.AllocateThirty(p,PlayerClassIds.Warrior);
            int native=PassiveTreeDefinition.NodeId("tree.v3.class.warrior.t01.left.a");Assert.That(p.TryRefund(native),Is.True);
            int subclass=PassiveTreeDefinition.ChoiceNodes("tree.v3.class.warrior.t01.left").First(id=>PassiveTreeDefinition.Node(id).IsSubclassChoice);
            Assert.That(p.TrySpend(subclass),Is.True);
            Assert.That(p.TrySpend(ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).Last()),Is.True);
            Assert.That(p.TrySelectClassRoute(PlayerClassIds.Barbarian),Is.True);
            Assert.That(p.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Barbarian,1)),Is.True);
            Assert.That(p.RestoreProgression(100,0,p.AvailablePoints,p.CopyRanks(),p.SelectedClassRoutes.ToArray(),p.SelectedWeaponTreeId),Is.True);
            var ui=uiRoot.AddComponent<SkillTreeUI>();var view=uiRoot.AddComponent<PassiveTreeView>();
            var popup=new GameObject("Choices",typeof(RectTransform),typeof(PassiveChoicePopupView)).GetComponent<PassiveChoicePopupView>();popup.transform.SetParent(uiRoot.transform);
            popup.title=new GameObject("Title",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();popup.title.transform.SetParent(popup.transform);
            popup.close=new GameObject("Close",typeof(RectTransform),typeof(Button)).GetComponent<Button>();popup.close.transform.SetParent(popup.transform);
            for(int i=0;i<4;i++){var b=new GameObject("Option",typeof(RectTransform),typeof(Button)).GetComponent<Button>();b.transform.SetParent(popup.transform);new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).transform.SetParent(b.transform);popup.options.Add(b);}
            view.choicePopup=popup;Field(ui,"authoredView",view);Field(ui,"progression",p);openField.SetValue(null,true);
            foreach(string side in new[]{"left","right"})
            {
                var slot=new GameObject(side,typeof(RectTransform),typeof(PassiveChoiceSlotView)).GetComponent<PassiveChoiceSlotView>();slot.transform.SetParent(uiRoot.transform);slot.choiceGroupId=$"tree.v3.class.barbarian.t01.{side}";
                Assert.DoesNotThrow(()=>typeof(SkillTreeUI).GetMethod("OpenSlot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{slot}));
                Assert.That(popup.options[0].interactable,Is.True);Assert.That(popup.options[3].gameObject.activeSelf,Is.False);
                popup.options[0].onClick.Invoke();Assert.That(p.IsAllocated(PassiveTreeDefinition.NodeId(slot.choiceGroupId+".a")),Is.True);
            }
        }
        finally{openField.SetValue(null,wasOpen);typeof(GameManager).GetProperty("Instance").SetValue(null,previous);Object.DestroyImmediate(uiRoot);Object.DestroyImmediate(managerRoot);}
    }

    [TestCase(1f)] [TestCase(0f)] public void ClosingActiveChallengeSelectorRestoresItsPreviousClock(float prior)
    {
        float original=Time.timeScale;var host=new GameObject("Pause regression");
        try
        {
            var service=host.AddComponent<ChallengeRuntimeService>();
            typeof(ChallengeRuntimeService).GetProperty("ActiveChallenge").SetValue(service,WorldContentCatalog.Reference.challengeEncounters[0]);
            var launcher=host.AddComponent<ChallengeLauncherUI>();
            Field(launcher,"ownsPause",true);Field(launcher,"previousTimeScale",prior);Time.timeScale=0;
            launcher.Close();Assert.That(Time.timeScale,Is.EqualTo(prior));
        }
        finally{Object.DestroyImmediate(host);Time.timeScale=original;}
    }
    [Test] public void UnlockedRoutesSurviveRefundAndRestore()
    {
        var root=new GameObject("Playtest regression");
        try
        {
            root.AddComponent<PlayerIdentityState>().BeginNewGame(PlayerClassIds.Warrior);
            var p=root.GetComponent<PlayerProgression>()??root.AddComponent<PlayerProgression>();
            Assert.That(p.RestoreProgression(100,0,100,new int[PassiveTreeDefinition.NodeCount]),Is.True);
            ClassKeystoneProgressionTests.AllocateThirty(p,PlayerClassIds.Warrior);
            int key=ClassPassiveProgressionRules.Keystones(PlayerClassIds.Warrior).First();
            Assert.That(p.TrySpend(key),Is.True);
            Assert.That(p.TrySelectClassRoute(PlayerClassIds.Barbarian),Is.True);
            Assert.That(p.TrySelectWeaponTree(WeaponTypeIds.Sword),Is.True);
            Assert.That(p.TryRefund(key),Is.True,"Unlock must not permanently pin the native keystone.");
            p.RefundAll();
            Assert.That(p.SelectedClassRoutes,Does.Contain(PlayerClassIds.Barbarian));
            Assert.That(p.SelectedWeaponTreeId,Is.EqualTo(WeaponTypeIds.Sword));
            Assert.That(p.RestoreProgression(100,0,100,p.CopyRanks(),p.SelectedClassRoutes.ToArray(),p.SelectedWeaponTreeId),Is.True);
            Assert.That(p.TrySpend(PassiveTreeDefinition.ClassSpineNode(PlayerClassIds.Barbarian,1)),Is.True);
            foreach(string side in new[]{"left","right"})
                Assert.That(p.TrySpend(PassiveTreeDefinition.NodeId($"tree.v3.class.barbarian.t01.{side}.a")),Is.True);
            Assert.That(p.TrySpend(PassiveTreeDefinition.WeaponSpineNode(WeaponTypeIds.Sword,1)),Is.True);
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test] public void PlayerHubIsRaycastable()
    {
        var view=AssetDatabase.LoadAssetAtPath<GameObject>(PassiveTreePrefabBuilder.PrefabPath).GetComponent<PassiveTreeView>();
        Assert.That(view.playerHub.GetComponent<Image>().raycastTarget,Is.True);
    }

    [Test] public void FreshBaselineHasNoInnateRecovery()
    {
        var root=new GameObject("Baseline regression");
        try{var stats=root.AddComponent<StatsComponent>();PlayerStatSetup.ApplyBaseline(stats);Assert.That(stats.GetStat(StatTypes.ManaRegeneration),Is.Zero);Assert.That(stats.GetStat(StatTypes.LifeRegeneration),Is.Zero);}
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void CritAndPrecisionMarkersOccupyOppositeSides()
    {
        var popup=new GameObject("Number regression",typeof(RectTransform));
        try
        {
            CritPopupMarker.Attach(popup,100);CritPopupMarker.Attach(popup,100,true);
            var crit=popup.transform.Find("Critical hit marker").GetComponent<RectTransform>();var precision=popup.transform.Find("Precision hit marker").GetComponent<RectTransform>();
            Assert.That(crit.anchoredPosition.x,Is.LessThan(0));Assert.That(precision.anchoredPosition.x,Is.GreaterThan(0));
            Assert.That(precision.GetComponent<Image>().color.g,Is.GreaterThan(precision.GetComponent<Image>().color.r));
            Assert.That(DamagePopup.FanOffset(0),Is.EqualTo(Vector2.zero));Assert.That(DamagePopup.FanOffset(1).x,Is.LessThan(0));Assert.That(DamagePopup.FanOffset(2).x,Is.GreaterThan(0));Assert.That(DamagePopup.FanOffset(3).y,Is.GreaterThan(DamagePopup.FanOffset(1).y));
        }
        finally{Object.DestroyImmediate(popup);}
    }
    [Test] public void MixedHitSpawnsThreeTypedNumbersAndLosesLifeOnce()
    {
        var recipient=new GameObject("Mixed damage regression");var popupHost=new GameObject("Popup regression");var template=new GameObject("Number",typeof(RectTransform),typeof(TextMeshProUGUI));
        try
        {
            var stats=recipient.AddComponent<StatsComponent>();stats.SetBaseStat(StatTypes.Life,1000);
            var health=recipient.AddComponent<HealthComponent>();health.ConfigureIsolatedStats(stats);
            var receiver=recipient.AddComponent<DamageReceiver>();var canvas=popupHost.AddComponent<Canvas>();var popup=popupHost.AddComponent<DamagePopup>();
            Field(popup,"popupPrefab",template);Field(popup,"popupRoot",popupHost.GetComponent<RectTransform>());Field(popup,"canvas",canvas);
            // EditMode does not invoke the production Awake/Start lifecycle.
            Field(receiver,"health",health);Field(receiver,"damagePopup",popup);
            var context=new DamageContext(3){IsCrit=true,IsPrecision=true};context.AddDamage(Element.Phys,10);context.AddDamage(Element.Fire,20);context.AddDamage(Element.Light,30);
            receiver.TakeDamage(60,context);
            Assert.That(health.CurrentLife,Is.EqualTo(940));Assert.That(popupHost.transform.childCount,Is.EqualTo(3));
            Assert.That(popupHost.GetComponentsInChildren<DamagePopupInstance>().Length,Is.EqualTo(3));
        }
        finally{Object.DestroyImmediate(recipient);Object.DestroyImmediate(popupHost);Object.DestroyImmediate(template);}
    }

    [Test] public void InventoryScrollOwnsClippedGrid()
    {
        var view=AssetDatabase.LoadAssetAtPath<GameObject>(InventoryAuthoringBuilder.PrefabPath).GetComponent<InventoryView>();
        var scroll=view.itemGridRoot.GetComponentInParent<ScrollRect>(true);
        Assert.That(scroll.content,Is.EqualTo(view.itemGridRoot));
        Assert.That(scroll.viewport.GetComponent<RectMask2D>(),Is.Not.Null);
        Assert.That(view.itemGridRoot.IsChildOf(scroll.viewport),Is.True);
    }
}
