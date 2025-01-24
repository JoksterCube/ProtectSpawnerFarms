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

        if (!IsOn.IsOn())
        {
            ResetActiveState();
            return;
        }

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

        if(ToggleSelectedOnlyShortcut.Value.IsKeyDown())
        {
            SelectedOnly.Value = SelectedOnly.Value.Not();

            plugin.Config.Save();

            PlayerExtensions.FormatedCenterMessage(SelectedOnlyToggleMessage, SelectedOnly.Value.ToString());

            return;
        }

        if (ResetHealthToOriginalShortcut.Value.IsKeyDown())
        {
            ToggleActiveState(ActiveState.Reset);

            PlayerExtensions.FormatedCenterMessage(ResetHealthMessage, IsCurrentActiveState(ActiveState.Reset).ToToggle().ToString());

            return;
        }

        if (SelectSpawnerShortcut.Value.IsKeyDown())
        {
            ToggleActiveState(ActiveState.Select);

            PlayerExtensions.FormatedCenterMessage(SelectSpawnerMessage, IsCurrentActiveState(ActiveState.Select).ToToggle().ToString());

            return;
        }

        if (DeselectSpawnerShortcut.Value.IsKeyDown())
        {
            ToggleActiveState(ActiveState.Deselect);

            PlayerExtensions.FormatedCenterMessage(DeselectSpawnerMessage, IsCurrentActiveState(ActiveState.Deselect).ToToggle().ToString());

            return;
        }
    }
}
