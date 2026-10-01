// Developer map: Persisted player preferences for whether opening gameplay windows pauses combat.
using UnityEngine;

public enum GameplayWindow
{
    PassiveTree
}

public static class GameplayOptions
{
    public static bool ContinueRunningWhileUnfocused
    {
        get => PlayerPrefs.GetInt("BlackCube.Options.RunUnfocused",0)!=0;
        set { PlayerPrefs.SetInt("BlackCube.Options.RunUnfocused",value?1:0);PlayerPrefs.Save();Application.runInBackground=value; }
    }
    public static bool WeaponSkillTooltips
    {
        get => PlayerPrefs.GetInt("BlackCube.Options.SkillTooltips",1)!=0;
        set { PlayerPrefs.SetInt("BlackCube.Options.SkillTooltips",value?1:0);PlayerPrefs.Save(); }
    }
    public const string AutoRestartOnDeathKey="BlackCube.Options.AutoRestartOnDeath";
    public static bool AutoRestartOnDeath
    {
        get=>PlayerPrefs.GetInt(AutoRestartOnDeathKey,0)!=0;
        set{PlayerPrefs.SetInt(AutoRestartOnDeathKey,value?1:0);PlayerPrefs.Save();}
    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public static bool UpgradeDiagnostics
    {
        get=>PlayerPrefs.GetInt("BlackCube.Options.UpgradeDiagnostics",0)!=0;
        set{PlayerPrefs.SetInt("BlackCube.Options.UpgradeDiagnostics",value?1:0);PlayerPrefs.Save();}
    }
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySettings() => Application.runInBackground=ContinueRunningWhileUnfocused;
    public const string PausePassiveTreeKey = "BlackCube.Options.PausePassiveTree";

    public static bool PausePassiveTree
    {
        get => PlayerPrefs.GetInt(PausePassiveTreeKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(PausePassiveTreeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // Keep window policy centralized so later menus can opt in without coupling
    // their UI-open state directly to the combat and animation clocks.
    public static bool PauseWhenOpen(GameplayWindow window) => window switch
    {
        GameplayWindow.PassiveTree => PausePassiveTree,
        _ => false
    };
}
