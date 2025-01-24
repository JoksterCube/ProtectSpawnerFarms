using BepInEx.Configuration;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;
using ServerSync;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Groups;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.SpawnerIds;

namespace JoksterCube.ProtectSpawnerFarms.Settings;

public static class PluginConfig
{
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    public static ConfigEntry<Toggle> IsOn = null!;

    public static ConfigEntry<KeyboardShortcut> ToggleShortcut;
    public static ConfigEntry<KeyboardShortcut> ToggleBoostShortcut;
    public static ConfigEntry<KeyboardShortcut> TogglePreventDamageShortcut;
    public static ConfigEntry<KeyboardShortcut> ToggleSelectedOnlyShortcut;
    public static ConfigEntry<KeyboardShortcut> ResetHealthToOriginalShortcut;
    public static ConfigEntry<KeyboardShortcut> SelectSpawnerShortcut;
    public static ConfigEntry<KeyboardShortcut> DeselectSpawnerShortcut;

    public static ConfigEntry<Toggle> PreventDamage = null!;
    public static ConfigEntry<Toggle> Boost = null!;
    public static ConfigEntry<Toggle> SelectedOnly = null!;
    public static ConfigEntry<float> BoostHealth = null!;
    public static ConfigEntry<float> DamagePreventionRange = null!;
    public static ConfigEntry<Toggle> ShowSideMessages = null!;

    public static ConfigEntry<Toggle> ToggleGreydwarfNest = null!;
    public static ConfigEntry<Toggle> ToggleEvilBonePile = null!;
    public static ConfigEntry<Toggle> ToggleBodyPile = null!;
    public static ConfigEntry<Toggle> ToggleMonumentOfTorment = null!;
    public static ConfigEntry<Toggle> ToggleMonumentOfTormentElite = null!;
    public static ConfigEntry<Toggle> ToggleEffigyOfMalice = null!;

    public static ConfigEntry<string> SelectedSpawners = null!;

    internal static ActiveState CurrentActiveState { get; private set; } = ActiveState.None;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        _serverConfigLocked = ConfigOptions.Config(General.Name, General.Lock.Name, Toggle.On, General.Lock.Description);
        _ = configSync.AddLockingConfigEntry(_serverConfigLocked);

        IsOn = ConfigOptions.Config(General.Name, General.IsOn.Name, Toggle.On, General.IsOn.Description);

        ToggleShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.ToggleOn.Name, new KeyboardShortcut(KeyCode.P, KeyCode.RightControl, KeyCode.RightShift), Shortcuts.ToggleOn.Description, false);
        ToggleBoostShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.ApplyBoost.Name, new KeyboardShortcut(KeyCode.O, KeyCode.RightControl), Shortcuts.ApplyBoost.Description, false);
        TogglePreventDamageShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.PreventDamage.Name, new KeyboardShortcut(KeyCode.P, KeyCode.RightControl), Shortcuts.PreventDamage.Description, false);
        ToggleSelectedOnlyShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.SelectedOnly.Name, new KeyboardShortcut(KeyCode.L, KeyCode.RightControl), Shortcuts.SelectedOnly.Description, false);

        ResetHealthToOriginalShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.ResetHealth.Name, new KeyboardShortcut(KeyCode.R, KeyCode.RightControl), Shortcuts.ResetHealth.Description, false);
        SelectSpawnerShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.SelectSpawner.Name, new KeyboardShortcut(KeyCode.K, KeyCode.RightControl), Shortcuts.SelectSpawner.Description, false);
        DeselectSpawnerShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.DeselectSpawner.Name, new KeyboardShortcut(KeyCode.J, KeyCode.RightControl), Shortcuts.DeselectSpawner.Description, false);

        PreventDamage = ConfigOptions.Config(Setting.Name, Setting.PreventDamage.Name, Toggle.On, Setting.PreventDamage.Description, false);
        Boost = ConfigOptions.Config(Setting.Name, Setting.Boost.Name, Toggle.Off, Setting.Boost.Description);
        SelectedOnly = ConfigOptions.Config(Setting.Name, Setting.SelectOnly.Name, Toggle.Off, Setting.SelectOnly.Description);
        ShowSideMessages = ConfigOptions.Config(Setting.Name, Setting.ShowSideMessages.Name, Toggle.On, Setting.ShowSideMessages.Description, false);

        BoostHealth = ConfigOptions.Config(Setting.Name, Setting.BoostHealth.Name, 10000f, Setting.BoostHealth.Description);

        DamagePreventionRange = ConfigOptions.Config(Setting.Name, Setting.DamagePreventionRange.Name, 25f,
                         new ConfigDescription(Setting.DamagePreventionRange.Description,
                         new AcceptableValueRange<float>(1f, 100f)));


        ToggleGreydwarfNest = ConfigOptions.Config(Spawners.Name, Spawners.GreydwarfNest.Name, Toggle.On, Spawners.GreydwarfNest.Description, false);
        ToggleEvilBonePile = ConfigOptions.Config(Spawners.Name, Spawners.EvilBonePile.Name, Toggle.On, Spawners.EvilBonePile.Description, false);
        ToggleBodyPile = ConfigOptions.Config(Spawners.Name, Spawners.BodyPile.Name, Toggle.On, Spawners.BodyPile.Description, false);
        ToggleMonumentOfTorment = ConfigOptions.Config(Spawners.Name, Spawners.MonumentOfTorment.Name, Toggle.On, Spawners.MonumentOfTorment.Description, false);
        ToggleMonumentOfTormentElite = ConfigOptions.Config(Spawners.Name, Spawners.MonumentOfTormentElite.Name, Toggle.On, Spawners.MonumentOfTormentElite.Description, false);
        ToggleEffigyOfMalice = ConfigOptions.Config(Spawners.Name, Spawners.EffigyOfMalice.Name, Toggle.On, Spawners.EffigyOfMalice.Description, false);

        SelectedSpawners = ConfigOptions.Config(Selection.Name, Selection.SelectedSpawners.Name, string.Empty, Selection.SelectedSpawners.Description);
    }
    internal static bool IsProtectedSpawnerType(string name)
    {
        if (name == GreydwarfNest) return ToggleGreydwarfNest.IsOn();
        if (name == EvilBonePile) return ToggleEvilBonePile.IsOn();
        if (name == BodyPile) return ToggleBodyPile.IsOn();
        if (name == MonumentOfTorment) return ToggleMonumentOfTorment.IsOn();
        if (name == MonumentOfTormentElite) return ToggleMonumentOfTormentElite.IsOn();
        if (name == EffigyOfMalice) return ToggleEffigyOfMalice.IsOn();
        return false;
    }

    internal static bool IsInRange(Vector3 position)
    {
        var playerPosition = Player.m_localPlayer.transform.position;
        var distance = Vector3.Distance(position, playerPosition);

        return distance <= DamagePreventionRange.Value;
    }

    internal static HashSet<long> ProtectionList => SelectedSpawners.Value
        .Split(',')
        .Where(x => long.TryParse(x, out _))
        .Select(long.Parse)
        .ToHashSet();

    internal static void AddToProtectionList(long value)
    {
        var newList = ProtectionList;
        newList.Add(value);
        SelectedSpawners.Value = string.Join(",", newList);
        Plugin.Instance.Config.Save();
    }

    internal static void RemoveFromProtectionList(long value)
    {
        var newList = ProtectionList;
        newList.Remove(value);
        SelectedSpawners.Value = string.Join(",", newList);
        Plugin.Instance.Config.Save();
    }

    internal static bool IsInSeletionList(long id) => ProtectionList.Contains(id);

    internal static void ResetActiveState() => ChangeActiveState(ActiveState.None);

    internal static void ChangeActiveState(ActiveState value) => CurrentActiveState = value;

    internal static void ToggleActiveState(ActiveState value) => ChangeActiveState(value == CurrentActiveState ? ActiveState.None : value);

    internal static bool IsCurrentActiveState(ActiveState value) => CurrentActiveState == value;
}