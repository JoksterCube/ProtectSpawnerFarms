namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Who may protect and unprotect spawners.
internal enum ProtectionPermission
{
    // Everyone with the mod.
    Anyone = 0,
    // Only server admins (and the host).
    AdminsOnly = 1,
    // Anyone can protect; only the player who protected a spawner, or an admin, can remove its protection.
    ProtectorOrAdmin = 2
}

internal static class ProtectionPermissions
{
    internal enum Verdict
    {
        Allowed,
        AdminsOnly,
        ProtectorOnly
    }

    // protectorId is who protected the spawner (empty when it isn't protected or that isn't known).
    internal static Verdict Check(ProtectionPermission permission, bool protect, string protectorId, string requesterId, bool isAdmin)
    {
        if (isAdmin || permission == ProtectionPermission.Anyone) return Verdict.Allowed;
        if (permission == ProtectionPermission.AdminsOnly) return Verdict.AdminsOnly;
        // ProtectorOrAdmin: protecting is open to all; unprotecting needs the same player. Spawners protected before
        // ids were kept have no protector id, so anyone may unprotect those.
        if (protect || string.IsNullOrEmpty(protectorId) || protectorId == requesterId) return Verdict.Allowed;
        return Verdict.ProtectorOnly;
    }
}
