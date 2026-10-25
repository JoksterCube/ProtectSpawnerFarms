using System;
using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

internal static class CompendiumPatches
{
    private const string Feature = "Compendium page";

    // Adds the Protected spawners page right after Active effects.
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
    private static class AddPage
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try
            {
                int index = Math.Min(1, __instance.m_texts.Count);
                __instance.m_texts.Insert(index, new TextsDialog.TextInfo(Compendium.Topic, Compendium.BuildText()));
            }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }

    // "Show on map" buttons.
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Update))]
    private static class MapButtons
    {
        private static void Postfix(TextsDialog __instance)
        {
            if (PatchGuard.IsDisabled(Feature)) return;
            try { Compendium.UpdateButtons(__instance); }
            catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
        }
    }
}
