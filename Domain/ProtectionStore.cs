using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Common.Networking;
using JoksterCube.ProtectSpawnerFarms.Settings;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Plugin;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

internal enum ShareMode
{
    // Not in a world, or sharing with other players is turned off: this game's own list only.
    Alone,
    // This game is the server (dedicated, hosting, or single player): its list is the shared one.
    Host,
    // Joined a server that has the mod: the server's list, sent by the server.
    Server,
    // Joined a server without the mod: this game's own list, merged with other players who have the mod.
    Players
}

// Which spawners are protected, and their history. Lists are saved per world: on the server when it has the mod,
// otherwise on each player's computer.
internal static class ProtectionStore
{
    private static readonly SpawnerRecords Local = new();
    private static readonly SpawnerRecords Shared = new();
    private static string? _localWorldId;
    private static string? _localFile;

    // Bumped on every change, so visuals refresh at once instead of on their next check.
    internal static int Revision { get; private set; }

    internal static bool SharedListReceived { get; private set; }

    internal static ShareMode Mode
    {
        get
        {
            if (!ZNet.instance) return ShareMode.Alone;
            if (ZNet.instance.IsServer()) return ShareMode.Host;
            if (RpcHandlers.ServerHasMod) return ShareMode.Server;
            return PluginConfig.ShareWithPlayers.IsOn() ? ShareMode.Players : ShareMode.Alone;
        }
    }

    internal static bool UsesServerList => Mode == ShareMode.Server;

    internal static SpawnerRecords Current => UsesServerList ? Shared : LocalRecords();

    internal static bool IsProtected(string key) => Current.IsProtected(key);

    // Real-world time, for showing when things happened.
    internal static long Now => DateTime.UtcNow.Ticks;

    // Game time: the network clock that everyone in a world shares, so the order of changes doesn't depend on
    // anyone's computer clock.
    internal static long GameNow => ZNet.instance ? (long)(ZNet.instance.GetTimeSeconds() * TimeSpan.TicksPerSecond) : 0L;

    // True when the server has the mod and keeps protected spawners' health up itself (Server Protection).
    internal static bool ServerProtects =>
        (Mode == ShareMode.Server || Mode == ShareMode.Host) && PluginConfig.ServerProtection.IsOn();

    internal static string LocalPlayerName =>
        Game.instance && Game.instance.GetPlayerProfile() != null ? Game.instance.GetPlayerProfile().GetName() : "?";

    // This character's id. A server with the mod uses each player's platform id instead (see ProtectionSync).
    internal static string LocalPlayerId =>
        Game.instance && Game.instance.GetPlayerProfile() != null
            ? Game.instance.GetPlayerProfile().GetPlayerID().ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;

    // Changes to this game's own list (the server's list, when this game is the server).
    internal static SpawnerRecord? SetLocal(string key, bool isProtected, string who, string whoId) =>
        Saved(LocalRecords().SetProtected(key, isProtected, who, whoId, GameNow, Now));

    internal static SpawnerRecord? TouchLocal(string key, TouchKind kind, string who) =>
        Saved(LocalRecords().Touch(key, kind, who, Now));

    // Records from other players; returns the ones that changed this list.
    internal static List<SpawnerRecord> MergeLocal(IEnumerable<SpawnerRecord> records)
    {
        SpawnerRecords local = LocalRecords();
        List<SpawnerRecord> changed = records.Where(local.Merge).ToList();
        if (changed.Count > 0) SaveLocal();
        return changed;
    }

    internal static List<string> LocalLines() => LocalRecords().ToLines();

    internal static void ReceiveShared(IEnumerable<SpawnerRecord> records, bool fullList)
    {
        if (fullList) Shared.Clear();
        foreach (SpawnerRecord record in records) Shared.Merge(record);
        if (fullList) SharedListReceived = true;
        Revision++;
    }

    internal static void ClearSession()
    {
        Local.Clear();
        Shared.Clear();
        _localWorldId = null;
        _localFile = null;
        SharedListReceived = false;
        Revision++;
    }

    private static SpawnerRecord? Saved(SpawnerRecord? changed)
    {
        if (changed != null) SaveLocal();
        return changed;
    }

    private static SpawnerRecords LocalRecords()
    {
        World? world = ZNet.instance ? ZNet.instance.GetWorld() : null;
        if (world == null)
        {
            if (_localWorldId != null) ClearSession();
            return Local;
        }

        string worldId = $"{world.m_name}_{world.m_uid}";
        if (worldId == _localWorldId) return Local;

        _localWorldId = worldId;
        _localFile = Path.Combine(Paths.ConfigPath, DataFolderName, SafeFileName(worldId) + ".txt");
        try
        {
            Local.Load(File.Exists(_localFile) ? File.ReadAllLines(_localFile) : [], Now);
            Plugin.ModLogger.LogInfo($"Loaded {Local.ProtectedCount} protected spawners for world {world.m_name}.");
        }
        catch (Exception exception)
        {
            Local.Clear();
            Plugin.ModLogger.LogError($"Could not read {_localFile}: {exception.Message}");
        }
        Revision++;
        return Local;
    }

    private static void SaveLocal()
    {
        Revision++;
        if (_localFile == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_localFile)!);
            string[] header =
            [
                "# Protected spawners for this world, one per line, tab separated:",
                "# key (prefab@x,y,z), protected (1/0), changed, protected by, protected at, guarded by, last guarded, last blocked hit. Times are UTC ticks."
            ];
            File.WriteAllLines(_localFile, header.Concat(Local.ToLines()));
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Could not save {_localFile}: {exception.Message}");
        }
    }

    private static string SafeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
