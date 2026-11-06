using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Every loaded spawner: a destructible object that spawns creatures (it has a SpawnArea on itself or a child).
// This finds all of them, including ones added by game updates (Deep North holes, black ice cores) or other mods.
internal static class SpawnerRegistry
{
    internal sealed class Spawner
    {
        internal readonly Destructible Destructible;
        internal readonly ZNetView View;
        internal readonly string Key;
        internal readonly string Name;
        internal GameObject? Bubble;
        internal Color BubbleColor;
        internal float BubbleOpacity;
        // Ownership is claimed to block damage; see SpawnerProtection.KeepOwnership.
        internal float ClaimedAt;
        // Guarded and blocked notes are sent at most this often.
        internal float NextGuardNote;
        internal float NextBlockNote;
        internal readonly HashSet<long> OtherProtectors = [];

        internal Spawner(Destructible destructible, ZNetView view, string key, string name)
        {
            Destructible = destructible;
            View = view;
            Key = key;
            Name = name;
        }

        internal bool IsAlive => Destructible && View && View.IsValid();
        internal Vector3 Position => Destructible.transform.position;
    }

    private static readonly Dictionary<Destructible, Spawner> Spawners = [];
    private static readonly List<Destructible> Removed = [];
    // Spawner prefabs by hash, with their default health, worked out once per world from the game's prefabs.
    private static readonly Dictionary<int, Destructible> Prefabs = [];
    private static ZNetScene? _prefabsFrom;

    internal static IEnumerable<Spawner> All => Spawners.Values;

    internal static void TryAdd(Destructible destructible)
    {
        if (!destructible.GetComponentInChildren<SpawnArea>(true)) return;
        ZNetView view = destructible.GetComponent<ZNetView>();
        if (!view || view.GetZDO() == null) return;

        string prefab = view.GetPrefabName();
        Spawners[destructible] = new Spawner(destructible, view, KeyOf(prefab, view.GetZDO()), DisplayName(destructible, prefab));
    }

    private static string KeyOf(string prefab, ZDO zdo)
    {
        Vector3 position = zdo.GetPosition();
        return SpawnerKey.For(prefab, position.x, position.y, position.z);
    }

    // Whether a world object is a spawner, from its data only, so it also works for spawners that aren't loaded.
    internal static bool IsSpawner(ZDO zdo) => Prefab(zdo.GetPrefab()) != null;

    // The spawner's key from its data; null when it isn't a spawner.
    internal static string? KeyOf(ZDO zdo)
    {
        Destructible? prefab = Prefab(zdo.GetPrefab());
        return prefab ? KeyOf(prefab!.name, zdo) : null;
    }

    // The game's own health for a kind of spawner (world level included).
    internal static float DefaultHealth(int prefabHash)
    {
        Destructible? prefab = Prefab(prefabHash);
        return prefab ? DefaultHealth(prefab!) : 0f;
    }

    private static Destructible? Prefab(int prefabHash)
    {
        if (!ZNetScene.instance) return null;
        if (_prefabsFrom != ZNetScene.instance)
        {
            _prefabsFrom = ZNetScene.instance;
            Prefabs.Clear();
            foreach (KeyValuePair<int, GameObject> entry in ZNetScene.instance.m_namedPrefabs)
                if (entry.Value && entry.Value.TryGetComponent(out Destructible destructible) && entry.Value.GetComponentInChildren<SpawnArea>(true))
                    Prefabs[entry.Key] = destructible;
        }
        return Prefabs.TryGetValue(prefabHash, out Destructible found) ? found : null;
    }

    internal static bool TryGet(Destructible destructible, out Spawner spawner) =>
        Spawners.TryGetValue(destructible, out spawner) && spawner.IsAlive;

    // Drops spawners that were destroyed or unloaded.
    internal static void Prune()
    {
        foreach (KeyValuePair<Destructible, Spawner> entry in Spawners)
            if (!entry.Value.IsAlive) Removed.Add(entry.Key);
        foreach (Destructible destructible in Removed) Spawners.Remove(destructible);
        Removed.Clear();
    }

    internal static Spawner? Closest(Vector3 position, float maxDistance)
    {
        Spawner? closest = null;
        float best = maxDistance * maxDistance;
        foreach (Spawner spawner in Spawners.Values)
        {
            if (!spawner.IsAlive) continue;
            float distance = (spawner.Position - position).sqrMagnitude;
            if (distance > best) continue;
            best = distance;
            closest = spawner;
        }
        return closest;
    }

    internal static void Clear()
    {
        Spawners.Clear();
        Prefabs.Clear();
        _prefabsFrom = null;
    }

    // The game's own health for this spawner, as Destructible works it out (world level included).
    internal static float DefaultHealth(Destructible destructible)
    {
        float health = destructible.m_health;
        return Game.instance ? health + Game.m_worldLevel * health * Game.instance.m_worldLevelMineHPMultiplier : health;
    }

    // The spawner's in-game name for a prefab name, also for spawners that aren't loaded.
    internal static string NameOf(string prefab)
    {
        GameObject? spawner = ZNetScene.instance ? ZNetScene.instance.GetPrefab(prefab) : null;
        Destructible? destructible = spawner ? spawner!.GetComponent<Destructible>() : null;
        return destructible ? DisplayName(destructible!, prefab) : prefab.Replace("Spawner_", "").Replace('_', ' ');
    }

    private static string DisplayName(Destructible destructible, string prefab)
    {
        HoverText hover = destructible.GetComponent<HoverText>();
        if (hover && !string.IsNullOrEmpty(hover.m_text) && Localization.instance != null)
            return Localization.instance.Localize(hover.m_text);
        return prefab.Replace("Spawner_", "").Replace('_', ' ');
    }
}
