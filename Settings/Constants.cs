namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "ProtectSpawnerFarms";
        internal const string ModVersion = "1.1.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Protect your spawners used in farming from accidental damage.";
        internal const string Copyright = "Copyright ©  2025";
        internal const string Guid = "53b674a6-219f-461c-9ac8-ddeb4570dd1f";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
    }

    internal static class DisplayMessages
    {
        internal const string PluginToggleMessage = "ProtectSpawnerFarms plugin: {0}";
        internal const string ProtectSpawnersToggleMessage = "Protecting spawners: {0}";
        internal const string BoostToggleMessage = "Boosting spawner health to {0}: {1}";
        internal const string SpawnersProtected = "{0} protected!";
        internal const string SpawnersHealthBoosted = "Spawner health boosted to {0}!";
        internal const string SelectedOnlyToggleMessage = "Protecting selected spawners only: {0}";
        internal const string ResetHealthMessage = "Reset spawner health: {0}";
        internal const string SelectSpawnerMessage = "Add spawner to select list: {0}";
        internal const string DeselectSpawnerMessage = "Remove spawner from select list: {0}";
        internal const string AddedToList = "Added spawner to protection list!";
        internal const string RemovedFromList = "Removed spawner from protection list!";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal static readonly string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class Groups
    {
        internal static class General
        {
            internal const string Name = "1 - General";

            internal static readonly ConfigInfo Lock = new(
                "Lock Configuration",
                "If on, the configuration is locked and can be changed by server admins only.");

            internal static readonly ConfigInfo IsOn = new(
                "Is ON",
                "Plugin is currently on or off.");
        }

        internal static class Shortcuts
        {
            internal const string Name = "2 - Shortcuts";

            internal static readonly ConfigInfo ToggleOn = new(
                "Toggle Keyboard Shortcut",
                "Keyboard shortcut to toggle behaviour.");

            internal static readonly ConfigInfo ApplyBoost = new(
                "Boost Keyboard Shortcut",
                "Keyboard shortcut to apply boost.");

            internal static readonly ConfigInfo PreventDamage = new(
                "Toggle Damage Prevention Keyboard Shortcut",
                "Keyboard shortcut to toggle damage protection.");

            internal static readonly ConfigInfo SelectedOnly = new(
                "Toggle Selected Spawners Protection only Keyboard Shortcut",
                "Keyboard shortcut to toggle damage protection damange prevention for select spawners only.");

            internal static readonly ConfigInfo ResetHealth = new(
                "Reset Health To Original Maximum",
                "Keyboard shortcut set next attacked spawner health to original maximum.");

            internal static readonly ConfigInfo SelectSpawner = new(
                "Select Spawner Keyboard Shortcut",
                "Keyboard shortcut to add next attacked spawner to selected spawner list.");

            internal static readonly ConfigInfo DeselectSpawner = new(
                "Deselect Spawner Keyboard Shortcut",
                "Keyboard shortcut to remove next attacked spawner from selected spawner list.");
        }

        internal static class Setting
        {
            internal const string Name = "3 - Settings";

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

            internal static readonly ConfigInfo SelectOnly = new(
                "Toggle Selected Spawners Only",
                "Toggle selected spawner damage protection only.");
        }

        internal static class Spawners
        {
            internal const string Name = "4 - Spawners";

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

        internal static class Selection
        {
            internal const string Name = "5 - Selection";

            internal static readonly ConfigInfo SelectedSpawners = new(
                "Selected Spawners",
                "List of spawners that are selected for damage protection.");
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
