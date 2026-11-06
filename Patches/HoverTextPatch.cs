using System;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

// Looking at a protected spawner shows who protected it under its name.
[HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
internal static class HoverTextPatch
{
    private const string Feature = "Spawner hover text";
    private const string Color = "#8fd3ff";

    private static void Postfix(HoverText __instance, ref string __result)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            if (!__instance.TryGetComponent(out Destructible destructible)
                || !SpawnerRegistry.TryGet(destructible, out SpawnerRegistry.Spawner spawner)
                || !SpawnerData.IsProtected(spawner.View.GetZDO())) return;

            string protector = SpawnerData.ProtectedBy(spawner.View.GetZDO());
            string line = string.IsNullOrEmpty(protector)
                ? Messages.HoverProtected
                : string.Format(Messages.HoverProtectedBy, protector);
            __result += $"\n<color={Color}>{line}</color>";
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
