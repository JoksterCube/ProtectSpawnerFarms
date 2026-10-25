using System;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;
using JoksterCube.ProtectSpawnerFarms.Domain.Networking;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

internal static class SessionPatches
{
    // ZNet makes a new routed RPC handler for every world or server session.
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    private static class RegisterRpcs
    {
        private const string Feature = "Protection sync";

        private static void Postfix()
        {
            if (PatchGuard.IsDisabled(Feature) || ZRoutedRpc.instance == null) return;
            try { ProtectionSync.Register(ZRoutedRpc.instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // ZNet is destroyed whenever a world or server session ends (logout, disconnect, server shutdown).
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    private static class ResetSession
    {
        private const string Feature = "Session reset";

        private static void Postfix()
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { SessionState.Clear(); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
