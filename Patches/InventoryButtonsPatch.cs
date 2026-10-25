using System;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

// Shows the protect and highlight buttons while the inventory is open near a spawner.
[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
internal static class InventoryButtonsPatch
{
    private const string Feature = "Inventory buttons";

    private static void Postfix(InventoryGui __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { SpawnerButtons.Update(__instance); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
