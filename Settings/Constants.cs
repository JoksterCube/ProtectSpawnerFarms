using BepInEx.Configuration;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "ProtectSpawnerFarms";
        internal const string ModVersion = "2.0.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Protect the spawners your farms are built around: no damage while you're near, raised health, a shield bubble, and a Compendium list shared with your friends.";
        internal const string Copyright = "Copyright ©  2026";
        internal const string Guid = "53b674a6-219f-461c-9ac8-ddeb4570dd1f";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
        // Protected spawner lists, one file per world.
        internal const string DataFolderName = ModGUID;
        internal const string VersionCheckRpc = $"{ModName}_VersionCheck";
    }

    internal static class Rpcs
    {
        // Client -> server: send me the protected list.
        internal const string RequestList = $"{Plugin.ModName}_RequestList";
        // Client -> server: protect or unprotect one spawner.
        internal const string SetProtected = $"{Plugin.ModName}_SetProtected";
        // Client -> server: a protected spawner was guarded, or blocked a hit.
        internal const string Touch = $"{Plugin.ModName}_Touch";
        // Server -> client: the full protected list, or one change.
        internal const string ProtectedList = $"{Plugin.ModName}_ProtectedList";
        // Server -> client: a change was refused.
        internal const string Refused = $"{Plugin.ModName}_Refused";
        // Player <-> player through a server without the mod: lists and changes.
        internal const string PlayerRecords = $"{Plugin.ModName}_PlayerRecords";
    }

    internal static class Messages
    {
        internal const string Protected = "{0} is now protected";
        internal const string Unprotected = "{0} is no longer protected";
        internal const string SharedSuffix = " for everyone with the mod";
        internal const string SharedWithPlayersSuffix = ", shared with other players who have the mod";
        internal const string HighlightOn = "Highlighting the nearest spawner";
        internal const string HighlightOff = "Stopped highlighting spawners";
        internal const string RefusedAdminsOnly = "Only server admins can change spawner protection";
        internal const string RefusedProtectorOnly = "Only {0} or an admin can remove this protection";
        internal const string StatusName = "Protecting spawner";
        internal const string StatusTooltip = "A protected spawner is near. Damage to it is blocked while you stay close.";
        internal const string ServerStatusName = "Protected spawner nearby";
        internal const string ServerStatusTooltip = "The server keeps this spawner safe from all damage.";
        internal const string HoverProtectedBy = "Protected by {0}";
        internal const string HoverProtected = "Protected";
        internal const string ProtectTooltipTopic = "Protect spawner";
        internal const string UnprotectTooltipTopic = "Unprotect spawner";
        internal const string ProtectTooltip = "{0}, {1} m away";
        internal const string UnprotectTooltip = "{0}, {1} m away";
        internal const string HighlightTooltipTopic = "Highlight nearest spawner";
        internal const string CompendiumTopic = "Protected spawners";
        internal const string CompendiumEmpty = "No spawners are protected in this world yet. Open your inventory next to a spawner and click the shield.";
        internal const string CompendiumCount = "{0} protected, nearest first.";
        internal const string CompendiumDistance = "  -  {0} away";
        internal const string CompendiumProtectedBy = "Protected by {0} {1}";
        internal const string CompendiumLastGuarded = "Last guarded {0} by {1}";
        internal const string CompendiumNeverGuarded = "Not guarded yet";
        internal const string CompendiumLastBlocked = "Last blocked a hit {0}";
        internal const string CompendiumNeverBlocked = "No hits blocked yet";
        internal const string CompendiumShowOnMap = "Show on map ({0:0}, {1:0})";
        internal const string CompendiumUnknownPlayer = "someone";
        internal const string CompendiumUnknownTime = "at an unknown time";
        internal const string CompendiumSharedByServer = "Kept by the server and shared with everyone who has the mod.";
        internal const string CompendiumSharedWithPlayers = "Saved on this computer and shared with other players who have the mod while you play together.";
        internal const string CompendiumHosted = "Saved with this world and shared with players who join with the mod.";
        internal const string CompendiumLocal = "Saved on this computer for this world.";
        internal const string StopHighlightTooltipTopic = "Stop highlighting";
        internal const string HighlightTooltip = "{0}, {1} m away";
        internal const string StopHighlightTooltip = "{0}, {1} m away";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigValues called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class Groups
    {
        internal static class Protection
        {
            internal const string Group = "1 - Protection";

            internal static readonly ConfigInfo<float> BoostedHealth = new(
                Group,
                "Boosted Health",
                new ConfigDescription(
                    "Health a protected spawner is raised to, so players without the mod don't destroy it by accident. " +
                    "Set to 0 to keep normal health. Unprotecting a spawner puts its health back to normal.",
                    new AcceptableValueRange<float>(0f, 1000000f)),
                10000f,
                true);

            internal static readonly ConfigInfo<float> ProtectionRange = new(
                Group,
                "Protection Range",
                new ConfigDescription(
                    "While you are within this many meters of a protected spawner, all damage to it is blocked, from anyone. " +
                    "You also get the Protecting spawner status.",
                    new AcceptableValueRange<float>(5f, 100f)),
                20f,
                true);

            internal static readonly ConfigInfo<Toggle> ShareWithPlayers = new(
                Group,
                "Share With Players",
                "On a server without the mod: share your protected spawners with other players who have the mod, and take in theirs. " +
                "Changes from anyone with the mod apply to everyone's list. Not used on a server with the mod, which keeps one list for everyone.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<ProtectionPermission> WhoCanChange = new(
                Group,
                "Who Can Change Protection",
                "Who may protect and unprotect spawners. Anyone: every player with the mod. AdminsOnly: only server admins. " +
                "ProtectorOrAdmin: anyone can protect, but only the player who protected a spawner (or an admin) can remove its protection. " +
                "On a server with the mod the server decides; without it, each player's own setting applies to their own changes.",
                ProtectionPermission.Anyone,
                true);

            internal static readonly ConfigInfo<Toggle> ServerProtection = new(
                Group,
                "Server Protection",
                "On a server with the mod: the server itself keeps protected spawners at full health, so nobody can destroy them, " +
                "with or without the mod and whether or not a player with the mod is near.",
                Toggle.On,
                true);
        }

        internal static class Controls
        {
            internal const string Group = "2 - Controls";

            internal static readonly ConfigInfo<float> ActionDistance = new(
                Group,
                "Action Distance",
                new ConfigDescription(
                    "The spawner buttons show in the inventory screen when a spawner is within this many meters. They act on the closest one.",
                    new AcceptableValueRange<float>(1f, 50f)),
                20f,
                false);
        }

        internal static class Display
        {
            internal const string Group = "3 - Display";

            internal static readonly ConfigInfo<Toggle> ShowShieldBubble = new(
                Group,
                "Show Shield Bubble",
                "Show a shield bubble, like the Staff of Protection's, around protected spawners.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Color> ShieldBubbleColor = new(
                Group,
                "Shield Bubble Color",
                "Color of the shield bubble around protected spawners.",
                new Color(0.2f, 0.55f, 1f, 1f),
                false);

            internal static readonly ConfigInfo<float> ShieldBubbleOpacity = new(
                Group,
                "Shield Bubble Opacity",
                new ConfigDescription(
                    "How solid the shield bubble looks: 0 is invisible, 1 is fully solid.",
                    new AcceptableValueRange<float>(0.01f, 1f)),
                0.1f,
                false);

            internal static readonly ConfigInfo<Color> HighlightColor = new(
                Group,
                "Highlight Color",
                "Color a highlighted spawner is painted in. It pulses and shows through walls.",
                new Color(1f, 0.75f, 0.2f, 1f),
                false);

            internal static readonly ConfigInfo<Toggle> ShowProtectingStatus = new(
                Group,
                "Show Protecting Status",
                "Show the Protecting spawner status while you are near a protected spawner.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowMessages = new(
                Group,
                "Show Messages",
                "Show a message when a spawner is protected, unprotected or highlighted.",
                Toggle.On,
                false);
        }
    }
}
