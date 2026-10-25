using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Item icons from the game, so the mod ships no art of its own.
internal static class Icons
{
    internal static readonly string[] Shield = ["ShieldWood", "ShieldBronzeBuckler", "ShieldWoodTower"];
    internal static readonly string[] Eye = ["GreydwarfEye", "SurtlingCore"];

    internal static Sprite? Find(string[] itemNames)
    {
        if (!ObjectDB.instance) return null;
        foreach (string itemName in itemNames)
        {
            GameObject? item = ObjectDB.instance.GetItemPrefab(itemName);
            ItemDrop? drop = item ? item!.GetComponent<ItemDrop>() : null;
            Sprite? icon = drop ? drop!.m_itemData.GetIcon() : null;
            if (icon) return icon;
        }
        return null;
    }
}
