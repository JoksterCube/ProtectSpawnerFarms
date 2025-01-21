using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Settings;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

[HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
public class PreventSpawnerDamagePatch
{
    static bool Prefix(Destructible __instance, ZNetView ___m_nview, ref HitData hit)
    {
        if (!PluginConfig.IsOn.IsOn() || !PluginConfig.PreventDamage.IsOn()) return true;

        if (__instance.name.Contains("GreydwarfNest"))
        {
            var currentHealth = ___m_nview.GetZDO().GetFloat(ZDOVars.s_health);
            var fullHealth = PluginConfig.Boost.IsOn()
                ? PluginConfig.BoostHealth.Value
                : __instance.m_health;
            var newHealth = Mathf.Max(fullHealth, currentHealth);

            ___m_nview.GetZDO().Set(ZDOVars.s_health, newHealth);

            return false;
        }

        return true;
    }
}