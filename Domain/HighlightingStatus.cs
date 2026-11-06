using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// The "Highlighting spawner" status, shown while the highlight follows the nearest spawner. Like ProtectingStatus, it
// lives only on this game.
internal static class HighlightingStatus
{
    private static StatusEffect? _effect;

    internal static void Set(Player? player, bool active)
    {
        if (!player) return;
        SEMan seMan = player!.GetSEMan();
        StatusEffect effect = ProtectingStatus.Effect(ref _effect, "SE_ProtectSpawnerFarms_Highlighting",
            Messages.HighlightStatusName, Messages.HighlightStatusTooltip, StatusIcons.Highlighting);
        bool has = seMan.HaveStatusEffect(effect.NameHash());
        if (active == has) return;
        if (active) seMan.AddStatusEffect(effect);
        else seMan.RemoveStatusEffect(effect.NameHash(), true);
    }
}
