using BepInEx.Configuration;
using JoksterCube.ProtectSpawnerFarms.Common;
using ServerSync;
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

    public static ConfigEntry<Toggle> PreventDamage = null!;
    public static ConfigEntry<Toggle> Boost = null!;
    public static ConfigEntry<float> BoostHealth = null!;
    public static ConfigEntry<float> DamagePreventionRange = null!;
    public static ConfigEntry<Toggle> ShowSideMessages = null!;

    public static ConfigEntry<Toggle> ToggleGreydwarfNest = null!;
    public static ConfigEntry<Toggle> ToggleEvilBonePile = null!;
    public static ConfigEntry<Toggle> ToggleBodyPile = null!;
    public static ConfigEntry<Toggle> ToggleMonumentOfTorment = null!;
    public static ConfigEntry<Toggle> ToggleMonumentOfTormentElite = null!;
    public static ConfigEntry<Toggle> ToggleEffigyOfMalice = null!;


    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        _serverConfigLocked = ConfigOptions.Config(General.Name, General.Lock.Name, Toggle.On, General.Lock.Description);
        _ = configSync.AddLockingConfigEntry(_serverConfigLocked);

        IsOn = ConfigOptions.Config(General.Name, General.IsOn.Name, Toggle.On, General.IsOn.Description);

        ToggleShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.ToggleOn.Name, new KeyboardShortcut(KeyCode.P, KeyCode.RightControl, KeyCode.RightShift), Shortcuts.ToggleOn.Description, false);
        ToggleBoostShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.ApplyBoost.Name, new KeyboardShortcut(KeyCode.O, KeyCode.RightControl), Shortcuts.ApplyBoost.Description, false);
        TogglePreventDamageShortcut = ConfigOptions.Config(Shortcuts.Name, Shortcuts.PreventDamage.Name, new KeyboardShortcut(KeyCode.P, KeyCode.RightControl), Shortcuts.PreventDamage.Description, false);

        PreventDamage = ConfigOptions.Config(Setting.Name, Setting.PreventDamage.Name, Toggle.On, Setting.PreventDamage.Description, false);
        Boost = ConfigOptions.Config(Setting.Name, Setting.Boost.Name, Toggle.Off, Setting.Boost.Description);
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
}