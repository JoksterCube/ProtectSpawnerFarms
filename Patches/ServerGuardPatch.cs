using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

// Server Protection reacts as soon as a player's changes reach the server, so a dropping health is put back at once.
[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RPC_ZDOData))]
internal static class ServerGuardPatch
{
    private static void Postfix() => ServerGuard.OnDataReceived();
}
