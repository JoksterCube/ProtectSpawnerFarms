using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using static JoksterCube.ProtectSpawnerFarms.Domain.Utilities;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.DisplayMessages;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

[HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
public class PreventSpawnerDamagePatch
{
    static bool Prefix(Destructible __instance, ZNetView ___m_nview, ref HitData hit)
    {
        if (!IsOn.IsOn()) return true;

        var internalId = ___m_nview.GetPrefabName();

        if (PreventDamage.IsOn() && IsProtectedSpawnerType(internalId) && IsInRange(__instance.transform.position))
        {
            var fullHealth = __instance.m_health;

            if (Boost.IsOn())
            {
                fullHealth = BoostHealth.Value;

                if (ShowSideMessages.IsOn())
                {
                    PlayerExtensions.FormatedTopLeftMessage(SpawnersHealthBoosted, fullHealth);
                }
            }

            var currentHealth = ___m_nview.GetZDO().GetFloat(ZDOVars.s_health);

            ___m_nview.GetZDO().Set(ZDOVars.s_health, Mathf.Max(fullHealth, currentHealth));

            if (ShowSideMessages.IsOn())
            {
                PlayerExtensions.FormatedTopLeftMessage(SpawnersProtected, internalId);
            }

            return false;
        }
        return true;
    }
}