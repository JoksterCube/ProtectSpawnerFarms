namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "ProtectSpawnerFarms";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Protect your spawners used in farming from accidental damage.";
        internal const string Copyright = "Copyright ©  2025";
        internal const string Guid = "53b674a6-219f-461c-9ac8-ddeb4570dd1f";

        internal static string ConfigFileName = $"{ModGUID}.cfg";
    }

    internal static class DisplayMessages
    {
        internal const string PluginToggleMessage = "PrtectSpawnerFarms plugin: {0}";
        internal const string ProtectSpawnersToggleMessage = "Protecting spawners: {0}";
        internal const string BoostToggleMessage = "Boosting spawner health to {0}: {1}";
        internal const string SpawnersProtected = "{0} protected!";
        internal const string SpawnersHealthBoosted = "Spawner health boosted to {0}!";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal static string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class Groups
    {
        internal static class General
        {
            public const string Name = "1 - General";

            internal static readonly ConfigInfo Lock = new(
                "Lock Configuration",
                "If on, the configuration is locked and can be changed by server admins only.");

            internal static readonly ConfigInfo IsOn = new(
                "Is ON",
                "Plugin is currently on or off.");
        }

        internal static class Shortcuts
        {
            public const string Name = "2 - Shortcuts";

            internal static readonly ConfigInfo ToggleOn = new(
                "Toggle Keyboard Shortcut",
                "Keyboard shortcut to toggle behaviour.");

            internal static readonly ConfigInfo ApplyBoost = new(
                "Boost Keyboard Shortcut",
                "Keyboard shortcut to apply boost.");

            internal static readonly ConfigInfo PreventDamage = new(
                "Toggle Damage Prevention Keyboard Shortcut",
                "Keyboard shortcut to toggle damage protection.");
        }

        internal static class Setting
        {
            public const string Name = "3 - Settings";

            internal static readonly ConfigInfo PreventDamage = new(
                "Prevent Damage",
                "Toggle damage prevention for spawners.");

            internal static readonly ConfigInfo Boost = new(
                "Toggle Boost",
                "Toggle spawner health boost.");

            internal static readonly ConfigInfo ShowSideMessages = new(
                "Toggle Side Messages",
                "If enabled, displays message on the side when damage is prevented and more.");

            internal static readonly ConfigInfo BoostHealth = new(
                "Boost Health",
                "Spawners health after boost.");

            internal static readonly ConfigInfo DamagePreventionRange = new(
                "Damage Prevention Range",
                "Range from player for player for spawner damage protection.");
        }

        internal static class Spawners
        {
            public const string Name = "4 - Spawners";

            internal static readonly ConfigInfo GreydwarfNest = new(
                "Greydwarf Nests",
                "Apply effects to Greydwarf Nest.");

            internal static readonly ConfigInfo EvilBonePile = new(
                "Evil bone pile",
                "Apply effects to Evil bone pile.");

            internal static readonly ConfigInfo BodyPile = new(
                "Body pile",
                "Apply effects to Body pile.");

            internal static readonly ConfigInfo MonumentOfTorment = new(
                "Monument of Torment",
                "Apply effects to Monument of Torment.");

            internal static readonly ConfigInfo MonumentOfTormentElite = new(
                "Monument of Torment (Elite)",
                "Apply effects to Monument of Torment (Elite).");

            internal static readonly ConfigInfo EffigyOfMalice = new(
                "Effigy of Malice",
                "Apply effects to Effigy of Malice.");
        }
    }



    internal static class SpawnerIds
    {
        internal const string GreydwarfNest = "Spawner_GreydwarfNest";
        internal const string EvilBonePile = "BonePileSpawner";
        internal const string BodyPile = "Spawner_DraugrPile";
        internal const string MonumentOfTorment = "Spawner_CharredStone";
        internal const string MonumentOfTormentElite = "Spawner_CharredStone_Elite";
        internal const string EffigyOfMalice = "Spawner_CharredCross";
    }
}
