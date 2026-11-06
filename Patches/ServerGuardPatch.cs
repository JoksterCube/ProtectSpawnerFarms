using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

internal static class ServerGuardPatch
{
    // Server Protection reacts as soon as a player's changes reach the server, so a dropping health is put back at once.
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RPC_ZDOData))]
    private static class RestoreHealth
    {
        private static void Postfix() => ServerGuard.OnDataReceived();
    }

    // A player's game destroyed a protected spawner: the server keeps it and doesn't tell anyone else to remove it.
    // This is the same work the game does here, with the protected spawners taken out of the message.
    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RPC_RoutedRPC))]
    private static class RefuseDestroy
    {
        private static bool Prefix(ZRoutedRpc __instance, ZPackage pkg)
        {
            if (!__instance.m_server) return true;
            int start = pkg.GetPos();
            ZRoutedRpc.RoutedRPCData data = new();
            data.Deserialize(pkg);
            if (data.m_methodHash != ServerGuard.DestroyMethod)
            {
                pkg.SetPos(start);
                return true;
            }

            if (ServerGuard.FilterDestroy(data))
            {
                if (data.m_targetPeerID == __instance.m_id || data.m_targetPeerID == 0L) __instance.HandleRoutedRPC(data);
                if (data.m_targetPeerID != __instance.m_id) __instance.RouteRPC(data);
            }
            return false;
        }
    }
}
