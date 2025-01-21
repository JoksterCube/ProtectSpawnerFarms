namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "ProtectSpawnerFarms";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "";
        internal const string Copyright = "Copyright ©  2025";
        internal const string Guid = "53b674a6-219f-461c-9ac8-ddeb4570dd1f";

        internal static string ConfigFileName = $"{ModGUID}.cfg";
    }

    internal static class DisplayMessages
    {
        internal const string PluginToggleMessage = "PrtectSpawnerFarms plugin: {0}";
        internal const string ProtectSpawnersToggleMessage = "Protecting spawners: {0}";
    }

    internal static class Messages
    {
        internal static string ConnectionError = "";
    }

    internal static class DebugMessages
    {
        internal static string ReadConfigCalled = "ReadConfigValues called";
        internal static string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal static string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class Groups
    {
        internal static class General
        {
            internal const string Name = "1 - General";

            internal static class Lock
            {
                internal const string Name = "Lock Configuration";
                internal const string Description = "If on, the configuration is locked and can be changed by server admins only.";
            }

            internal static class IsOn
            {
                internal const string Name = "Is ON";
                internal const string Description = "Plugin is currently on or off.";
            }
        }

        internal static class Shortcuts
        {
            internal const string Name = "2 - Shortcuts";

            internal static class ToggleOn
            {
                internal const string Name = "Toggle Keyboard Shortcut";
                internal const string Description = "Keyboard shortcut to toggle behaviour.";
            }

            internal static class ApplyBoost
            {
                internal const string Name = "Boost Keyboard Shortcut";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }
            internal static class PreventDamage
            {
                internal const string Name = "Toggle Damage Prevention Keyboard Shortcut";
                internal const string Description = "Keyboard shortcut to toggle damage protection.";
            }
        }

        internal static class Setting
        {
            internal const string Name = "3 - Settings";

            internal static class PreventDamage
            {
                internal const string Name = "Prevent Damage";
                internal const string Description = "Toggle damage prevention for spawners.";
            }

            internal static class Boost
            {
                internal const string Name = "Toggle Boost";
                internal const string Description = "Toggle spawner health boost.";
            }

            internal static class BoostHealth
            {
                internal const string Name = "Boost Health";
                internal const string Description = "Spawners health after boost.";
            }

            internal static class DamagePreventionRange
            {
                internal const string Name = "Damage Prevention Range";
                internal const string Description = "Range from player for player for spawner damage protection.";
            }
        }

        internal static class Spawners
        {
            internal const string Name = "4 - Spawners";

            internal static class GreydwarfNests
            {
                internal const string Name = "Greydwarf Nests";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }

            internal static class EvilBonePile
            {
                internal const string Name = "Evil bone pile";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }

            internal static class BodyPile
            {
                internal const string Name = "Body pile";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }

            internal static class MonumentOfTorment
            {
                internal const string Name = "Monument of Torment";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }

            internal static class EffigyOfMalice
            {
                internal const string Name = "Effigy of Malice";
                internal const string Description = "Keyboard shortcut to apply boost.";
            }
        }
    }
}
