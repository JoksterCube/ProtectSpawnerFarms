using JoksterCube.ProtectSpawnerFarms.Settings;
using static MessageHud;

namespace JoksterCube.ProtectSpawnerFarms.Common;

internal static class PlayerMessages
{
    internal static void Center(string message)
    {
        if (Player.m_localPlayer && PluginConfig.ShowMessages.IsOn())
            Player.m_localPlayer.Message(MessageType.Center, message);
    }

    // Refusals are always shown, whatever Show Messages is set to.
    internal static void Warning(string message)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageType.Center, message);
    }
}
