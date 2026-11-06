using System;
using System.Collections.Generic;
using JoksterCube.ProtectSpawnerFarms.Common;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Server Protection: on a server with the mod, protected spawners can't be destroyed, by anyone.
//
// A hit is worked out by the game that owns the spawner, often a player without the mod, so the server can't stop
// the hit itself. Two things keep the spawner anyway:
// - Health: whenever a protected spawner's health drops, the server puts it back and marks its copy as clearly
//   newer, so every player, the one hitting it included, takes the server's health.
// - Destroying: a game that destroys an object tells the server, which removes it and tells everyone else. For a
//   protected spawner the server refuses: it keeps the spawner, doesn't pass the message on, and sends the spawner
//   back at full health to the player whose game destroyed it.
internal static class ServerGuard
{
    private const string Feature = "Server protection";
    // After data from a player arrives, at most this often.
    private const float MinCheckInterval = 0.1f;
    // Also checked on its own this often.
    private const float CheckInterval = 1f;

    internal static readonly int DestroyMethod = "DestroyZDO".GetStableHashCode();
    private static readonly List<ZDOID> Kept = [];
    private static readonly List<ZDOID> Destroyed = [];

    private static float _lastCheck = -1f;

    private static bool Active =>
        ZNet.instance && ZNet.instance.IsServer() && ZDOMan.instance != null && ZNetScene.instance;

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

    internal static void ClearSession() => _lastCheck = -1f;

    // A player's "destroy these objects" message, as it reaches the server. Takes the protected spawners out of it.
    // Returns false when nothing was left to destroy, so the message is dropped.
    internal static bool FilterDestroy(ZRoutedRpc.RoutedRPCData data)
    {
        if (data.m_methodHash != DestroyMethod || !data.m_targetZDO.IsNone()
            || PatchGuard.IsDisabled(Feature) || !Active) return true;
        try
        {
            data.m_parameters.SetPos(0);
            ZPackage list = data.m_parameters.ReadPackage();
            data.m_parameters.SetPos(0);
            Kept.Clear();
            Destroyed.Clear();
            int count = list.ReadInt();
            for (int i = 0; i < count; i++)
            {
                ZDOID id = list.ReadZDOID();
                if (SpawnerData.IsProtected(ZDOMan.instance.GetZDO(id))) Kept.Add(id);
                else Destroyed.Add(id);
            }
            if (Kept.Count == 0) return true;

            foreach (ZDOID id in Kept) Restore(ZDOMan.instance.GetZDO(id));
            Plugin.ModLogger.LogInfo($"Refused to destroy {Kept.Count} protected spawner(s), from player {data.m_senderPeerID}.");
            if (Destroyed.Count == 0) return false;

            ZPackage rest = new();
            rest.Write(Destroyed.Count);
            foreach (ZDOID id in Destroyed) rest.Write(id);
            ZPackage parameters = new();
            parameters.Write(rest);
            parameters.SetPos(0);
            data.m_parameters = parameters;
            return true;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            data.m_parameters.SetPos(0);
            return true;
        }
        finally
        {
            Kept.Clear();
            Destroyed.Clear();
        }
    }

    private static void Run()
    {
        if (PatchGuard.IsDisabled(Feature) || !Active) return;
        _lastCheck = Time.time;
        try { Check(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // The protected spawners are the ones the server's list found in the world (see ProtectedSpawners).
    private static void Check()
    {
        foreach (ZDOID id in ProtectedSpawners.TrackedIds)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || !SpawnerData.IsProtected(zdo)) continue;
            float target = TargetHealth(zdo);
            if (target <= 0f || zdo.GetFloat(ZDOVars.s_health, SpawnerRegistry.DefaultHealth(zdo.GetPrefab())) >= target) continue;
            zdo.Set(ZDOVars.s_health, target);
            SpawnerData.Written(zdo);
        }
    }

    // At least Boosted Health (never lowered), and a copy clearly newer than any player's, so the server sends it
    // again to everyone near, the player whose game just destroyed it included.
    private static void Restore(ZDO zdo)
    {
        float target = TargetHealth(zdo);
        float health = zdo.GetFloat(ZDOVars.s_health, SpawnerRegistry.DefaultHealth(zdo.GetPrefab()));
        if (target > 0f && health < target) zdo.Set(ZDOVars.s_health, target);
        zdo.DataRevision += SpawnerData.ServerRevisionLead;
    }

    private static float TargetHealth(ZDO zdo) =>
        Mathf.Max(BoostedHealth.Value, SpawnerRegistry.DefaultHealth(zdo.GetPrefab()));
}
