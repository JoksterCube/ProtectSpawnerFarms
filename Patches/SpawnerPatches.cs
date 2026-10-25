using System;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

internal static class SpawnerPatches
{
    private const string Feature = "Spawner damage protection";

    // Notes every spawner as it loads.
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
    private static class RegisterSpawner
    {
        private static void Postfix(Destructible __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { SpawnerRegistry.TryAdd(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // Your own hits (and those of creatures your game controls) on a protected spawner near you are never sent.
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    private static class BlockOutgoingHit
    {
        private static bool Prefix(Destructible __instance, HitData hit) => !Blocks(__instance, hit);
    }

    // Hits from anyone else are worked out by the spawner's owner; this game owns protected spawners near you.
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
    private static class BlockIncomingHit
    {
        private static bool Prefix(Destructible __instance, HitData hit) =>
            !__instance.m_nview || !__instance.m_nview.IsOwner() || !Blocks(__instance, hit);
    }

    private static bool Blocks(Destructible destructible, HitData hit)
    {
        if (PatchGuard.IsDisabled(Feature)) return false;
        try
        {
            if (!SpawnerProtection.TryBlockDamage(destructible)) return false;
            if (DamageText.instance && hit != null)
                DamageText.instance.ShowText(DamageText.TextType.Immune, hit.m_point, 0f);
            return true;
        }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return false;
        }
    }
}
