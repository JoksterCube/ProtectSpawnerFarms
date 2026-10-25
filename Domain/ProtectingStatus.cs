using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// The status shown while you are near a protected spawner. It lives only on this game: added to your own status
// list, never sent to other players.
// - "Protecting spawner": your presence is what keeps it safe (server without the mod).
// - "Protected spawner nearby": the server keeps it safe by itself (server with the mod, Server Protection on).
internal static class ProtectingStatus
{
    private static StatusEffect? _protecting;
    private static StatusEffect? _serverProtected;

    internal static void Set(Player? player, bool active, bool serverProtects)
    {
        if (!player) return;
        SEMan seMan = player!.GetSEMan();
        StatusEffect wanted = serverProtects
            ? Effect(ref _serverProtected, "SE_ProtectSpawnerFarms_ServerProtected", Messages.ServerStatusName, Messages.ServerStatusTooltip)
            : Effect(ref _protecting, "SE_ProtectSpawnerFarms_Protecting", Messages.StatusName, Messages.StatusTooltip);
        StatusEffect? other = serverProtects ? _protecting : _serverProtected;

        if (other && seMan.HaveStatusEffect(other!.NameHash())) seMan.RemoveStatusEffect(other.NameHash(), true);
        bool has = seMan.HaveStatusEffect(wanted.NameHash());
        if (active == has) return;
        if (active) seMan.AddStatusEffect(wanted);
        else seMan.RemoveStatusEffect(wanted.NameHash(), true);
    }

    private static StatusEffect Effect(ref StatusEffect? effect, string name, string displayName, string tooltip)
    {
        if (!effect)
        {
            effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = name;
            effect.m_name = displayName;
            effect.m_tooltip = tooltip;
            effect.m_ttl = 0f;
            effect.hideFlags = HideFlags.HideAndDontSave;
        }
        // The HUD only lists effects with an icon; ObjectDB may not have been ready the first time.
        if (!effect!.m_icon) effect.m_icon = Icons.Find(Icons.Shield);
        return effect;
    }
}
