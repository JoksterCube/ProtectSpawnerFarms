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
// - While you are near (Protection Range), every hit on them is blocked. Hits are worked out by the spawner's owner,
//   so this game takes ownership of a protected spawner while you are near it, unless the server already keeps it
//   safe by itself (Server Protection, see ServerGuard).
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
            || !ProtectionStore.IsProtected(spawner.Key)
            || !IsNearLocalPlayer(spawner)) return false;

        float now = Time.time;
        if (now >= spawner.NextBlockNote)
        {
            spawner.NextBlockNote = now + BlockNoteInterval;
            ProtectionSync.Touch(spawner.Key, TouchKind.Blocked);
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
            ProtectionSync.Update(now);
            if (now < _nextTick && ProtectionStore.Revision == _seenRevision) return;
            _nextTick = now + TickInterval;
            _seenRevision = ProtectionStore.Revision;
            Tick(now);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Protects the spawner if it isn't, or removes protection if it is.
    internal static void Toggle(Spawner spawner)
    {
        bool protect = !ProtectionStore.IsProtected(spawner.Key);
        if (!ProtectionSync.RequestSet(spawner.Key, protect)) return;
        if (protect) spawner.NextGuardNote = 0f;

        // Health follows right away; if the server refuses, the next check puts it back.
        if (!protect) RestoreHealth(spawner);
        else if (spawner.View.IsOwner() || TryClaim(spawner, Time.time)) RaiseHealth(spawner);

        string message = string.Format(protect ? Messages.Protected : Messages.Unprotected, spawner.Name);
        message += ProtectionStore.Mode switch
        {
            ShareMode.Server => Messages.SharedSuffix,
            ShareMode.Players => Messages.SharedWithPlayersSuffix,
            _ => string.Empty
        };
        PlayerMessages.Center(message);
    }

    private static void Tick(float now)
    {
        SpawnerRegistry.Prune();
        Player player = Player.m_localPlayer;
        bool near = false;
        bool serverProtects = ProtectionStore.ServerProtects;

        foreach (Spawner spawner in SpawnerRegistry.All)
        {
            bool isProtected = ProtectionStore.IsProtected(spawner.Key);
            ProtectedMarker.Update(spawner, isProtected);
            if (!isProtected) continue;

            if (spawner.View.IsOwner()) RaiseHealth(spawner);
            if (!player || !IsNearLocalPlayer(spawner)) continue;

            near = true;
            if (!serverProtects && !spawner.View.IsOwner()) KeepOwnership(spawner, now);
            if (now >= spawner.NextGuardNote)
            {
                spawner.NextGuardNote = now + GuardNoteInterval;
                ProtectionSync.Touch(spawner.Key, TouchKind.Guarded);
            }
        }

        ProtectingStatus.Set(player, near && ShowProtectingStatus.IsOn(), serverProtects);
    }

    // Takes ownership so this game decides about every hit. When two protecting players are near, the one that
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

        if (TryClaim(spawner, now)) RaiseHealth(spawner);
    }

    private static bool TryClaim(Spawner spawner, float now)
    {
        spawner.View.ClaimOwnership();
        spawner.ClaimedAt = now;
        return spawner.View.IsOwner();
    }

    private static void RaiseHealth(Spawner spawner)
    {
        float boosted = BoostedHealth.Value;
        if (boosted <= 0f) return;
        ZDO zdo = spawner.View.GetZDO();
        float health = zdo.GetFloat(ZDOVars.s_health, SpawnerRegistry.DefaultHealth(spawner.Destructible));
        if (health < boosted) zdo.Set(ZDOVars.s_health, boosted);
    }

    private static void RestoreHealth(Spawner spawner)
    {
        if (!spawner.View.IsOwner() && !TryClaim(spawner, Time.time)) return;
        ZDO zdo = spawner.View.GetZDO();
        float normal = SpawnerRegistry.DefaultHealth(spawner.Destructible);
        if (zdo.GetFloat(ZDOVars.s_health, normal) > normal) zdo.Set(ZDOVars.s_health, normal);
    }

    internal static void ClearSession()
    {
        _nextTick = 0f;
        _seenRevision = -1;
    }
}
