using System;
using System.Collections.Generic;
using JoksterCube.ProtectSpawnerFarms.Common;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Server Protection: on a server with the mod, the server itself keeps protected spawners at full health.
//
// A hit is worked out by the game that owns the spawner, often a player without the mod, so the server can't stop
// the hit. But every change to the spawner reaches the server, and the server can write over it: whenever a
// protected spawner's health drops, the server puts it back and marks its copy as clearly newer, so every player,
// the one hitting it included, takes the server's health. Destroying one would take Boosted Health worth of damage
// between two network updates.
internal static class ServerGuard
{
    private const string Feature = "Server protection";
    // After data from a player arrives, at most this often.
    private const float MinCheckInterval = 0.1f;
    // Also checked on its own this often.
    private const float CheckInterval = 1f;
    // How often to look again for protected spawners that weren't found (for example destroyed before protection).
    private const float MissingRetry = 10f;
    // How much newer the server's copy is marked, so it wins over the owner's own changes made in the meantime.
    private const uint RevisionLead = 100;

    private static readonly Dictionary<string, ZDOID> Index = [];
    private static readonly HashSet<string> IndexedKeys = [];
    private static readonly Dictionary<int, float> DefaultHealthByPrefab = [];
    private static readonly Dictionary<int, List<string>> Wanted = [];
    private static float _lastCheck = -1f;
    private static float _nextRetry;

    private static bool Active =>
        ZNet.instance && ZNet.instance.IsServer() && ServerProtection.IsOn() && ZDOMan.instance != null && ZNetScene.instance;

    // Every frame on the server.
    internal static void Update()
    {
        if (Time.time - _lastCheck >= CheckInterval) Run();
    }

    // When a player sent changes to the server.
    internal static void OnDataReceived()
    {
        if (Time.time - _lastCheck >= MinCheckInterval) Run();
    }

    internal static void ClearSession()
    {
        Index.Clear();
        IndexedKeys.Clear();
        DefaultHealthByPrefab.Clear();
        _lastCheck = -1f;
        _nextRetry = 0f;
    }

    private static void Run()
    {
        if (PatchGuard.IsDisabled(Feature) || !Active) return;
        _lastCheck = Time.time;
        try { Check(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void Check()
    {
        bool rebuild = false;
        foreach (SpawnerRecord record in ProtectionStore.Current.Protected)
        {
            // A newly protected spawner is looked up at once; one that wasn't found only now and then.
            if (Index.ContainsKey(record.Key)) continue;
            if (!IndexedKeys.Contains(record.Key) || Time.time >= _nextRetry) rebuild = true;
        }
        if (rebuild) RebuildIndex();

        foreach (SpawnerRecord record in ProtectionStore.Current.Protected)
        {
            if (!Index.TryGetValue(record.Key, out ZDOID id)) continue;
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null)
            {
                Index.Remove(record.Key);
                continue;
            }

            float normal = DefaultHealth(zdo.GetPrefab());
            float target = Mathf.Max(BoostedHealth.Value, normal);
            if (target <= 0f || zdo.GetFloat(ZDOVars.s_health, normal) >= target) continue;
            zdo.Set(ZDOVars.s_health, target);
            zdo.DataRevision += RevisionLead;
        }
    }

    // Finds the world objects of all protected spawners. Object ids change every time a world loads, so they are
    // found by prefab and position, the same way spawners are keyed.
    private static void RebuildIndex()
    {
        _nextRetry = Time.time + MissingRetry;
        Index.Clear();
        IndexedKeys.Clear();
        Wanted.Clear();
        foreach (SpawnerRecord record in ProtectionStore.Current.Protected)
        {
            IndexedKeys.Add(record.Key);
            if (!SpawnerKey.TryParse(record.Key, out string prefab, out _, out _, out _)) continue;
            int hash = prefab.GetStableHashCode();
            if (!Wanted.TryGetValue(hash, out List<string> keys)) Wanted[hash] = keys = [];
            keys.Add(record.Key);
        }
        if (Wanted.Count == 0) return;

        foreach (ZDO zdo in ZDOMan.instance.m_objectsByID.Values)
        {
            if (!Wanted.TryGetValue(zdo.GetPrefab(), out List<string> keys)) continue;
            Vector3 position = zdo.GetPosition();
            foreach (string key in keys)
            {
                SpawnerKey.TryParse(key, out string prefab, out _, out _, out _);
                if (SpawnerKey.For(prefab, position.x, position.y, position.z) == key) Index[key] = zdo.m_uid;
            }
        }
    }

    private static float DefaultHealth(int prefabHash)
    {
        if (DefaultHealthByPrefab.TryGetValue(prefabHash, out float health)) return health;
        GameObject prefab = ZNetScene.instance.GetPrefab(prefabHash);
        Destructible? destructible = prefab ? prefab.GetComponent<Destructible>() : null;
        health = destructible ? SpawnerRegistry.DefaultHealth(destructible!) : 0f;
        DefaultHealthByPrefab[prefabHash] = health;
        return health;
    }
}
