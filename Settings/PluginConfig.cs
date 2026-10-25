using BepInEx.Configuration;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;
using ServerSync;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Groups;
using Display = JoksterCube.ProtectSpawnerFarms.Settings.Constants.Groups.Display;

namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal static class PluginConfig
{
    internal static ConfigEntry<float> BoostedHealth = null!;
    internal static ConfigEntry<float> ProtectionRange = null!;
    internal static ConfigEntry<ProtectionPermission> WhoCanChange = null!;
    internal static ConfigEntry<Toggle> ServerProtection = null!;
    internal static ConfigEntry<Toggle> ShareWithPlayers = null!;

    internal static ConfigEntry<float> ActionDistance = null!;

    internal static ConfigEntry<Toggle> ShowShieldBubble = null!;
    internal static ConfigEntry<Color> ShieldBubbleColor = null!;
    internal static ConfigEntry<float> ShieldBubbleOpacity = null!;
    internal static ConfigEntry<Color> HighlightColor = null!;
    internal static ConfigEntry<Toggle> ShowProtectingStatus = null!;
    internal static ConfigEntry<Toggle> ShowMessages = null!;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        BoostedHealth = ConfigOptions.Config(Protection.BoostedHealth);
        ProtectionRange = ConfigOptions.Config(Protection.ProtectionRange);
        WhoCanChange = ConfigOptions.Config(Protection.WhoCanChange);
        ServerProtection = ConfigOptions.Config(Protection.ServerProtection);
        ShareWithPlayers = ConfigOptions.Config(Protection.ShareWithPlayers);

        ActionDistance = ConfigOptions.Config(Controls.ActionDistance);

        ShowShieldBubble = ConfigOptions.Config(Display.ShowShieldBubble);
        ShieldBubbleColor = ConfigOptions.Config(Display.ShieldBubbleColor);
        ShieldBubbleOpacity = ConfigOptions.Config(Display.ShieldBubbleOpacity);
        HighlightColor = ConfigOptions.Config(Display.HighlightColor);
        ShowProtectingStatus = ConfigOptions.Config(Display.ShowProtectingStatus);
        ShowMessages = ConfigOptions.Config(Display.ShowMessages);
    }
}
