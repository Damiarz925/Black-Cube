// Developer map: Persisted player preferences for whether opening gameplay windows pauses combat.
using UnityEngine;

public enum GameplayWindow
{
    PassiveTree
}

public static class GameplayOptions
{
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
