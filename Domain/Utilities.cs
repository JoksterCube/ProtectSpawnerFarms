using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.SpawnerIds;
using JoksterCube.ProtectSpawnerFarms.Common;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

internal static class Utilities
{
    internal static bool IsProtectedSpawnerType(string name)
    {
        Plugin.PluginLogger.LogInfo(name + " " +
            ToggleGreydwarfNest.IsOn() + " " +
            ToggleEvilBonePile.IsOn() + " " +
            ToggleBodyPile.IsOn() + " " +
            ToggleMonumentOfTorment.IsOn() + " " +
            ToggleMonumentOfTormentElite.IsOn() + " " +
            ToggleEffigyOfMalice.IsOn());

        if (name == GreydwarfNest) return ToggleGreydwarfNest.IsOn();
        if (name == EvilBonePile) return ToggleEvilBonePile.IsOn();
        if (name == BodyPile) return ToggleBodyPile.IsOn();
        if (name == MonumentOfTorment) return ToggleMonumentOfTorment.IsOn();
        if (name == MonumentOfTormentElite) return ToggleMonumentOfTormentElite.IsOn();
        if (name == EffigyOfMalice) return ToggleEffigyOfMalice.IsOn();
        return false;
    }

    internal static bool IsInRange(Vector3 position)
    {
        var playerPosition = Player.m_localPlayer.transform.position;
        var distance = Vector3.Distance(position, playerPosition);

        Plugin.PluginLogger.LogInfo(distance);

        return distance <= DamagePreventionRange.Value;
    }
}
