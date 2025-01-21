using JoksterCube.ProtectSpawnerFarms.Common;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.DisplayMessages;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

internal static class InputManager
{
    internal static void Update(Plugin plugin)
    {
        if (ToggleShortcut.Value.IsKeyDown())
        {
            IsOn.Value = IsOn.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(PluginToggleMessage, IsOn.Value.ToString());

            return;
        }

        if (!IsOn.IsOn()) return;

        if (TogglePreventDamageShortcut.Value.IsKeyDown())
        {
            PreventDamage.Value = PreventDamage.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(ProtectSpawnersToggleMessage, PreventDamage.Value.ToString());

            return;
        }

        if (ToggleBoostShortcut.Value.IsKeyDown())
        {
            Boost.Value = Boost.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(BoostToggleMessage, BoostHealth.Value.ToString(), Boost.Value.ToString());

            return;
        }
    }
}
