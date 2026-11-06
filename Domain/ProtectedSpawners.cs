using System;
using System.Collections.Generic;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Common.Networking;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

internal enum ShareMode
{
    // Not in a world.
    Alone,
    // This game is the server (dedicated, hosting, or single player): it holds the whole world's data.
    Host,
    // Joined a server that has the mod: the server sends the list.
    Server,
    // Joined a server without the mod: the list is what this game has seen itself.
    Players
}

// The protected spawners this game knows of, for the Compendium and Server Protection. Protection itself is saved
// on each spawner (see SpawnerData), so this list is only a view of it, never saved:
// - Host: every protected spawner in the world. A server holds all of the world's data, so it is looked through once
//   when the world starts, and after that the spawners found (and those protected since) are read again now and then.
// - Server: the server's list, which it sends to players with the mod, and every change to it.
// - Players: the protected spawners this game has loaded. It fills in as you get near them, and is remembered
//   between sessions, one file per server world (see CompendiumCache).
internal static class ProtectedSpawners
{
    private static readonly SpawnerRecords Records = new();
    // Where each listed spawner is in the world data, to read it again. Object ids hold for one session only.
    private static readonly Dictionary<string, ZDOID> Ids = new(StringComparer.Ordinal);
    private static readonly List<KeyValuePair<string, ZDOID>> Known = [];
    private static bool _scanned;
    // Players: the remembered list was read, and has changes not saved yet. Off once a server with the mod answers.
    private static bool _cacheLoaded;
    private static bool _cacheDirty;
    private static bool _cacheOff;

    // Bumped on every change, so visuals refresh at once instead of on their next check.
    internal static int Revision { get; private set; }

    internal static bool ListReceived { get; private set; }

    internal static ShareMode Mode
    {
        get
        {
            if (!ZNet.instance) return ShareMode.Alone;
            if (ZNet.instance.IsServer()) return ShareMode.Host;
            return RpcHandlers.ServerHasMod ? ShareMode.Server : ShareMode.Players;
        }
    }

    internal static IEnumerable<SpawnerRecord> All => Records.All;

    // The world objects of the protected spawners, for Server Protection.
    internal static IEnumerable<ZDOID> TrackedIds => Ids.Values;

    // Real-world time, for showing when things happened.
    internal static long Now => DateTime.UtcNow.Ticks;

    // True when the server has the mod, so it keeps protected spawners safe by itself (Server Protection).
    internal static bool ServerProtects => Mode == ShareMode.Server || Mode == ShareMode.Host;

    internal static string LocalPlayerName =>
        Game.instance && Game.instance.GetPlayerProfile() != null ? Game.instance.GetPlayerProfile().GetName() : "?";

    // This character's id. A server with the mod uses each player's platform id instead (see ProtectionSync).
    internal static string LocalPlayerId =>
        Game.instance && Game.instance.GetPlayerProfile() != null
            ? Game.instance.GetPlayerProfile().GetPlayerID().ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Empty;

    // Reads a spawner's data again. Returns the change to the list (a record, or one saying the spawner is no longer
    // protected), or null when nothing changed.
    internal static SpawnerRecord? Note(ZDO zdo, string key)
    {
        SpawnerRecord record = SpawnerData.Read(zdo, key);
        if (record.IsProtected) Ids[key] = zdo.m_uid;
        else Ids.Remove(key);
        return Changed(record);
    }

    // Reads the listed spawners again (the whole world the first time, on the Host), and returns what changed.
    internal static List<SpawnerRecord> Refresh()
    {
        List<SpawnerRecord> changes = [];
        if (ZDOMan.instance == null || !ZNetScene.instance) return changes;
        bool host = Mode == ShareMode.Host;
        if (host && !_scanned)
        {
            _scanned = true;
            Scan(changes);
            return changes;
        }

        Known.Clear();
        Known.AddRange(Ids);
        foreach (KeyValuePair<string, ZDOID> entry in Known)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(entry.Value);
            if (zdo != null)
            {
                if (Note(zdo, entry.Key) is { } change) changes.Add(change);
            }
            // A server holds every object, so a missing one was destroyed. A player only has the data of what it
            // has been near, so there the spawner stays listed.
            else if (host)
            {
                Ids.Remove(entry.Key);
                if (Changed(new SpawnerRecord(entry.Key) { IsProtected = false }) is { } change) changes.Add(change);
            }
        }
        Known.Clear();
        if (!host) UpdateCache();
        return changes;
    }

    // Reads the remembered list once you are in the world, and saves it when it changed (at most every refresh).
    private static void UpdateCache()
    {
        if (_cacheOff) return;
        if (!_cacheLoaded)
        {
            if (!Player.m_localPlayer || CompendiumCache.FileFor() is not { } file) return;
            _cacheLoaded = true;
            // Spawners already read this session are newer than what was remembered.
            foreach (SpawnerRecord record in CompendiumCache.Load(file))
                if (!Records.TryGet(record.Key, out _)) Records.Set(record);
            Revision++;
        }
        SaveCache();
    }

    private static void SaveCache()
    {
        if (!_cacheLoaded || !_cacheDirty || _cacheOff) return;
        _cacheDirty = false;
        CompendiumCache.Save(Records.All);
    }

    private static void Scan(List<SpawnerRecord> changes)
    {
        foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
        {
            if (!SpawnerData.IsProtected(zdo) || SpawnerRegistry.KeyOf(zdo) is not { } key) continue;
            if (Note(zdo, key) is { } change) changes.Add(change);
        }
        Plugin.ModLogger.LogInfo($"Found {Records.Count} protected spawners in this world.");
    }

    // Server: the full list, or changes to it.
    internal static void Receive(IEnumerable<SpawnerRecord> records, bool fullList)
    {
        // A server with the mod has the whole list, so nothing is remembered on this computer.
        _cacheOff = true;
        if (fullList) Records.Clear();
        foreach (SpawnerRecord record in records) Records.Set(record);
        if (fullList) ListReceived = true;
        Revision++;
    }

    private static SpawnerRecord? Changed(SpawnerRecord record)
    {
        if (!Records.Set(record)) return null;
        Revision++;
        _cacheDirty = true;
        return record;
    }

    internal static void ClearSession()
    {
        SaveCache();
        CompendiumCache.ClearSession();
        _cacheLoaded = _cacheDirty = _cacheOff = false;
        Records.Clear();
        Ids.Clear();
        Known.Clear();
        _scanned = false;
        ListReceived = false;
        Revision++;
    }
}
