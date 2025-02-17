﻿using HarmonyLib;
using JoksterCube.ProtectSpawnerFarms.Common;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.DisplayMessages;
using UnityEngine;
using JoksterCube.ProtectSpawnerFarms.Domain;

namespace JoksterCube.ProtectSpawnerFarms.Patches;

[HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
public class PreventSpawnerDamagePatch
{
    static bool Prefix(Destructible __instance, ZNetView ___m_nview, ref HitData hit)
    {
        var internalId = ___m_nview.GetPrefabName();
        if (!IsOn.IsOn() && !IsProtectedSpawnerType(internalId)) return true;

        var uid = ___m_nview.GetZDO().m_uid.ID;
        ActOnActiveState(uid);

        if (PreventDamage.IsOn() && (SelectedOnly.Value.IsOn() ? IsInSeletionList(uid) : IsInRange(__instance.transform.position)))
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

            if (IsCurrentActiveState(ActiveState.Reset))
            {
                ___m_nview.GetZDO().Set(ZDOVars.s_health, __instance.m_health);
            }
            else
            {
                ___m_nview.GetZDO().Set(ZDOVars.s_health, Mathf.Max(fullHealth, currentHealth));
            }

            if (ShowSideMessages.IsOn())
            {
                PlayerExtensions.FormatedTopLeftMessage(SpawnersProtected, internalId);
            }

            ResetActiveState();
            return false;
        }
        ResetActiveState();
        return true;
    }

    private static void ActOnActiveState(long uid)
    {
        switch (CurrentActiveState)
        {
            case ActiveState.None:
            case ActiveState.Reset:
                return;
            case ActiveState.Select:
                AddToProtectionList(uid);
                PlayerExtensions.FormatedTopLeftMessage(AddedToList, uid.ToString());
                break;
            case ActiveState.Deselect:
                RemoveFromProtectionList(uid);
                PlayerExtensions.FormatedTopLeftMessage(RemovedFromList, uid.ToString());
                break;
        }
    }
}