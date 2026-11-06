using System;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain.Networking;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using Spawner = JoksterCube.ProtectSpawnerFarms.Domain.SpawnerRegistry.Spawner;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Keeps protected spawners safe on this game:
// - Their health is raised (Boosted Health) by whoever owns them, so players without the mod can't destroy one by accident.
// - Every hit on them is blocked, wherever you are. Hits are worked out by the spawner's owner, so this game takes
//   ownership of every protected spawner it has loaded, unless the server already keeps it safe by itself
//   (Server Protection, see ServerGuard).
// - Being near one (Protection Range) only shows the status and notes that you guarded it.
internal static class SpawnerProtection
{
    private const string Feature = "Spawner protection";
    private const float TickInterval = 0.5f;
    // Losing ownership this soon after claiming it means another protecting player claimed it too.
    private const float ClaimContestWindow = 10f;
    private const float GuardNoteInterval = 300f;
    private const float BlockNoteInterval = 30f;

    private static float _nextTick;
    private static int _seenRevision = -1;

    internal static bool IsNearLocalPlayer(Spawner spawner)
    {
        Player player = Player.m_localPlayer;
        return player && Vector3.Distance(player.transform.position, spawner.Position) <= ProtectionRange.Value;
    }

    // True when the hit must be blocked; also notes the blocked hit in the spawner's history.
    internal static bool TryBlockDamage(Destructible destructible)
    {
        if (!SpawnerRegistry.TryGet(destructible, out Spawner spawner)
            || !SpawnerData.IsProtected(spawner.View.GetZDO())) return false;

        float now = Time.time;
        if (now >= spawner.NextBlockNote)
        {
            spawner.NextBlockNote = now + BlockNoteInterval;
            ProtectionSync.Touch(spawner, TouchKind.Blocked);
        }
        return true;
    }

    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        // A dedicated server only keeps and shares the list; it has no player, screen or nearby spawners.
        if (!ZNet.instance || ZNet.instance.IsDedicated()) return;
        try
        {
            SpawnerHighlight.Update();
            Compendium.Update();
            float now = Time.time;
            if (now < _nextTick && ProtectedSpawners.Revision == _seenRevision) return;
            _nextTick = now + TickInterval;
            _seenRevision = ProtectedSpawners.Revision;
            Tick(now);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Protects the spawner if it isn't, or removes protection if it is.
    internal static void Toggle(Spawner spawner)
    {
        ZDO zdo = spawner.View.GetZDO();
        bool protect = !SpawnerData.IsProtected(zdo);
        if (!ProtectionSync.RequestSet(spawner, protect)) return;
        if (protect) spawner.NextGuardNote = 0f;

        // Health follows right away on the spawner's owner. A server with the mod sets it itself once it accepts the
        // change, so a refused change never leaves an unprotected spawner with raised health.
        if (ProtectedSpawners.Mode != ShareMode.Server && spawner.View.IsOwner())
        {
            if (protect) RaiseHealth(zdo);
            else RestoreHealth(zdo);
        }
        PlayerMessages.Center(string.Format(protect ? Messages.Protected : Messages.Unprotected, spawner.Name));
    }

    private static void Tick(float now)
    {
        SpawnerRegistry.Prune();
        Player player = Player.m_localPlayer;
        bool near = false;
        bool serverProtects = ProtectedSpawners.ServerProtects;
        // A server with the mod sends the Compendium list; otherwise it is what this game sees on loaded spawners.
        bool noteLoaded = ProtectedSpawners.Mode != ShareMode.Server;

        foreach (Spawner spawner in SpawnerRegistry.All)
        {
            ZDO zdo = spawner.View.GetZDO();
            if (noteLoaded) ProtectedSpawners.Note(zdo, spawner.Key);
            bool isProtected = SpawnerData.IsProtected(zdo);
            ProtectedMarker.Update(spawner, isProtected);
            if (!isProtected) continue;

            if (spawner.View.IsOwner()) RaiseHealth(zdo);
            else if (!serverProtects) KeepOwnership(spawner, now);
            if (!player || !IsNearLocalPlayer(spawner)) continue;

            near = true;
            if (now >= spawner.NextGuardNote)
            {
                spawner.NextGuardNote = now + GuardNoteInterval;
                ProtectionSync.Touch(spawner, TouchKind.Guarded);
            }
        }

        ProtectingStatus.Set(player, near && ShowProtectingStatus.IsOn(), serverProtects);
    }

    // Takes ownership so this game decides about every hit. When two players with the mod have it loaded, the one that
    // claimed last keeps it and the other stops claiming, instead of both taking it back and forth.
    private static void KeepOwnership(Spawner spawner, float now)
    {
        long owner = spawner.View.GetZDO().GetOwner();
        if (spawner.OtherProtectors.Contains(owner)) return;

        if (spawner.ClaimedAt > 0f && now - spawner.ClaimedAt < ClaimContestWindow)
        {
            spawner.OtherProtectors.Add(owner);
            spawner.ClaimedAt = 0f;
            return;
        }

        spawner.View.ClaimOwnership();
        spawner.ClaimedAt = now;
        if (spawner.View.IsOwner()) RaiseHealth(spawner.View.GetZDO());
    }

    // Written by the spawner's owner, or by the server (see SpawnerData).
    internal static void RaiseHealth(ZDO zdo)
    {
        float boosted = BoostedHealth.Value;
        if (boosted <= 0f || zdo.GetFloat(ZDOVars.s_health, SpawnerRegistry.DefaultHealth(zdo.GetPrefab())) >= boosted) return;
        zdo.Set(ZDOVars.s_health, boosted);
        SpawnerData.Written(zdo);
    }

    internal static void RestoreHealth(ZDO zdo)
    {
        float normal = SpawnerRegistry.DefaultHealth(zdo.GetPrefab());
        if (normal <= 0f || zdo.GetFloat(ZDOVars.s_health, normal) <= normal) return;
        zdo.Set(ZDOVars.s_health, normal);
        SpawnerData.Written(zdo);
    }

    internal static void ClearSession()
    {
        _nextTick = 0f;
        _seenRevision = -1;
    }
}
