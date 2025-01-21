using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Settings;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

internal static class InputManager
{
    internal static void Update(Plugin plugin)
    {
        if (PluginConfig.ToggleShortcut.Value.IsKeyDown())
        {
            PluginConfig.IsOn.Value = PluginConfig.IsOn.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(Constants.DisplayMessages.PluginToggleMessage, PluginConfig.IsOn.Value.ToString());

            return;
        }

        if (!PluginConfig.IsOn.IsOn()) return;


        if (PluginConfig.TogglePreventDamageShortcut.Value.IsKeyDown())
        {
            PluginConfig.PreventDamage.Value = PluginConfig.PreventDamage.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(Constants.DisplayMessages.ProtectSpawnersToggleMessage, PluginConfig.PreventDamage.Value.ToString());

            return;
        }
    }
}
