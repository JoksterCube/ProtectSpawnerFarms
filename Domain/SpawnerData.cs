using JoksterCube.ProtectSpawnerFarms.Settings;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// A spawner's protection and its history are saved on the spawner itself, in its network data (ZDO), the way
// FuelChests marks its fuel chests. So they are saved with the world, every player with the mod reads them from the
// spawner, and they are kept even on servers without the mod. Players and servers without the mod keep the values
// without reading them.
//
// Only the spawner's owner writes them, or the server. The server's copy is then marked as clearly newer, so it wins
// over changes the owner made in the meantime (see ServerGuard).
internal static class SpawnerData
{
    private static readonly int ProtectedKey = Hash("Protected");
    private static readonly int ProtectedByKey = Hash("ProtectedBy");
    private static readonly int ProtectedByIdKey = Hash("ProtectedById");
    private static readonly int ProtectedAtKey = Hash("ProtectedAt");
    private static readonly int GuardedByKey = Hash("GuardedBy");
    private static readonly int LastGuardedKey = Hash("LastGuarded");
    private static readonly int LastBlockedKey = Hash("LastBlocked");

    internal const uint ServerRevisionLead = 100;

    private static int Hash(string name) => $"{Constants.Plugin.ModGUID}.{name}".GetStableHashCode();

    internal static bool IsProtected(ZDO? zdo) => zdo != null && zdo.GetBool(ProtectedKey);

    internal static string ProtectedBy(ZDO zdo) => zdo.GetString(ProtectedByKey);

    internal static string ProtectedById(ZDO zdo) => zdo.GetString(ProtectedByIdKey);

    // What the spawner says, as a record; key is the spawner's SpawnerKey.
    internal static SpawnerRecord Read(ZDO zdo, string key) => IsProtected(zdo)
        ? new SpawnerRecord(key)
        {
            ProtectedBy = zdo.GetString(ProtectedByKey),
            ProtectedById = zdo.GetString(ProtectedByIdKey),
            ProtectedAt = zdo.GetLong(ProtectedAtKey),
            GuardedBy = zdo.GetString(GuardedByKey),
            LastGuarded = zdo.GetLong(LastGuardedKey),
            LastBlocked = zdo.GetLong(LastBlockedKey)
        }
        : new SpawnerRecord(key) { IsProtected = false };

    internal static bool CanWrite(ZDO zdo) => zdo.IsOwner() || IsServer;

    // Protecting starts a fresh history; unprotecting clears it. Values are cleared by writing empty ones: the
    // game's Remove calls don't mark the data as changed, so a removal would never reach anyone else.
    // now is real-world UTC ticks.
    internal static void SetProtected(ZDO zdo, bool protect, string who, string whoId, long now)
    {
        zdo.Set(ProtectedKey, protect);
        zdo.Set(ProtectedByKey, protect ? SpawnerRecords.Clean(who) : string.Empty);
        zdo.Set(ProtectedByIdKey, protect ? SpawnerRecords.Clean(whoId) : string.Empty);
        zdo.Set(ProtectedAtKey, protect ? now : 0L);
        zdo.Set(GuardedByKey, string.Empty);
        zdo.Set(LastGuardedKey, 0L);
        zdo.Set(LastBlockedKey, 0L);
        Written(zdo);
    }

    // Notes that a protected spawner was guarded or blocked a hit. Returns false when nothing changed.
    internal static bool Touch(ZDO zdo, TouchKind kind, string who, long now)
    {
        if (!IsProtected(zdo)) return false;
        if (kind == TouchKind.Blocked)
        {
            if (now <= zdo.GetLong(LastBlockedKey)) return false;
            zdo.Set(LastBlockedKey, now);
        }
        else
        {
            if (now <= zdo.GetLong(LastGuardedKey)) return false;
            zdo.Set(LastGuardedKey, now);
            zdo.Set(GuardedByKey, SpawnerRecords.Clean(who));
        }
        Written(zdo);
        return true;
    }

    // Marks the server's copy as newer than the owner's, so the owner takes it instead of sending its own back.
    internal static void Written(ZDO zdo)
    {
        if (IsServer && !zdo.IsOwner()) zdo.DataRevision += ServerRevisionLead;
    }

    private static bool IsServer => ZNet.instance && ZNet.instance.IsServer();
}
