using JoksterCube.ProtectSpawnerFarms.Common.Networking;
using JoksterCube.ProtectSpawnerFarms.Domain.Networking;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Single place that forgets everything tied to one world/server connection, so joining another server or world
// starts clean. Synced settings are put back to this computer's own values by ServerSync when leaving a server.
internal static class SessionState
{
    internal static void Clear()
    {
        SpawnerHighlight.Stop();
        SpawnerRegistry.Clear();
        SpawnerButtons.ClearSession();
        Compendium.ClearSession();
        ServerGuard.ClearSession();
        SpawnerProtection.ClearSession();
        ProtectionSync.ClearSession();
        ProtectionStore.ClearSession();
        // The next server is checked again: a modded server must not carry over to an unmodded one.
        RpcHandlers.ResetServerVersionCheck();
    }
}
