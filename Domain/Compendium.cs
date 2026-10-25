using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// The "Protected spawners" page of the Compendium: every protected spawner in this world with its history,
// and a "Show on map" button that pins it and opens the map there, the way boss stones do.
// The text keeps an invisible placeholder where each button goes; the buttons are copies of the game's stone
// button laid over those places, as children of the text so they scroll with it.
internal static class Compendium
{
    private const string LinkPrefix = "psf:";
    private const string NameColor = "#ffd27f";
    private const string DateColor = "#9fd8ff";
    private const string FadedColor = "#a0a0a0";
    private const string PlaceColor = "#e8d9b0";
    private static readonly Vector2 ButtonPadding = new(28f, 10f);
    // Away from the panel's left edge and the line above.
    private static readonly Vector2 ButtonOffset = new(18f, -8f);

    private readonly struct MapTarget(Vector3 position, string name, string label)
    {
        internal Vector3 Position { get; } = position;
        internal string Name { get; } = name;
        // The button's text: "Show on map (100, -200)".
        internal string Label { get; } = label;
    }

    private static readonly List<MapTarget> Targets = [];
    private static MapTarget? _pending;
    private static readonly List<GameObject> Buttons = [];
    private static readonly List<(int Index, Vector2 Min, Vector2 Max)> Spots = [];

    internal static string Topic => Messages.CompendiumTopic;

    internal static string BuildText()
    {
        Targets.Clear();
        StringBuilder text = new(1024);
        text.Append("<color=#c8c8c8>").Append(SharingLine()).Append("</color>\n\n");

        Vector3? player = Player.m_localPlayer ? Player.m_localPlayer.transform.position : null;
        var entries = ProtectionStore.Current.Protected
            .Select(record => SpawnerKey.TryParse(record.Key, out string prefab, out float x, out float y, out float z)
                ? (Record: record, Prefab: prefab, Position: new Vector3(x, y, z))
                : default)
            .Where(entry => entry.Record != null)
            .OrderBy(entry => player.HasValue ? Vector3.Distance(player.Value, entry.Position) : 0f)
            .ToList();

        if (entries.Count == 0)
        {
            text.Append(Messages.CompendiumEmpty);
            return text.ToString();
        }

        text.Append(string.Format(Messages.CompendiumCount, entries.Count)).Append("\n\n");
        foreach (var (record, prefab, position) in entries)
        {
            string name = SpawnerRegistry.NameOf(prefab);
            // "Greydwarf Nest  -  1252 m away"
            text.Append("<color=orange>").Append(name).Append("</color>");
            if (player.HasValue)
                text.Append(string.Format(Messages.CompendiumDistance,
                    Colored(PlaceColor, Vector3.Distance(player.Value, position).ToString("0") + " m")));
            text.Append('\n');

            text.Append(string.Format(Messages.CompendiumProtectedBy, Who(record.ProtectedBy), When(record.ProtectedAt))).Append('\n');
            text.Append(record.LastGuarded > 0
                ? string.Format(Messages.CompendiumLastGuarded, When(record.LastGuarded), Who(record.GuardedBy))
                : Messages.CompendiumNeverGuarded).Append('\n');
            text.Append(record.LastBlocked > 0
                ? string.Format(Messages.CompendiumLastBlocked, When(record.LastBlocked))
                : Messages.CompendiumNeverBlocked).Append('\n');

            string label = string.Format(CultureInfo.InvariantCulture, Messages.CompendiumShowOnMap, position.x, position.z);
            // Invisible, a little larger than the text around it: it only keeps room for the button.
            text.Append("<size=120%><link=\"").Append(LinkPrefix).Append(Targets.Count).Append("\"><alpha=#00>   ")
                .Append(label).Append("   </link><alpha=#FF></size>\n\n");
            Targets.Add(new MapTarget(position, name, label));
        }
        // Something visible-sized after the last placeholder, so the text (and its scroll area) reaches below its button.
        text.Append("<alpha=#00>.<alpha=#FF>");
        return text.ToString();
    }

    // Called every frame while the Compendium is open. The buttons are placed again every frame from where the
    // text currently has each placeholder, so they follow the text's final layout; on other pages they are removed.
    internal static void UpdateButtons(TextsDialog dialog)
    {
        TMP_Text? area = dialog.m_textArea;
        TMP_Text? topic = dialog.m_textAreaTopic;
        if (!area || !topic) return;
        if (topic!.text != Topic)
        {
            if (Buttons.Count > 0) ClearButtons();
            return;
        }

        TMP_TextInfo info = area!.textInfo;
        Spots.Clear();
        float width = 0f;
        for (int link = 0; info != null && link < info.linkCount; link++)
        {
            TMP_LinkInfo linkInfo = info.linkInfo[link];
            string id = linkInfo.GetLinkID();
            if (id == null || !id.StartsWith(LinkPrefix, StringComparison.Ordinal)
                || !int.TryParse(id.Substring(LinkPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
                || index < 0 || index >= Targets.Count
                || !TryGetBounds(info, linkInfo, out Vector2 min, out Vector2 max)) continue;
            Spots.Add((index, min, max));
            width = Math.Max(width, max.x - min.x);
        }

        // Every button gets the widest placeholder's width, lined up on the left, so they all look the same.
        int placed = 0;
        foreach ((int index, Vector2 min, Vector2 max) in Spots)
        {
            while (Buttons.Count <= index)
            {
                GameObject? created = CreateButton(area, Buttons.Count);
                if (!created) return;
                Buttons.Add(created!);
            }
            Place(Buttons[index], min, new Vector2(min.x + width, max.y), Targets[index].Label);
            placed = Math.Max(placed, index + 1);
        }
        for (int index = 0; index < Buttons.Count; index++)
            if (index >= placed && Buttons[index] && Buttons[index].activeSelf) Buttons[index].SetActive(false);
    }

    private static bool TryGetBounds(TMP_TextInfo info, TMP_LinkInfo link, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);
        int end = Math.Min(link.linkTextfirstCharacterIndex + link.linkTextLength, info.characterCount);
        for (int index = Math.Max(0, link.linkTextfirstCharacterIndex); index < end; index++)
        {
            TMP_CharacterInfo character = info.characterInfo[index];
            min = Vector2.Min(min, new Vector2(character.origin, character.descender));
            max = Vector2.Max(max, new Vector2(character.xAdvance, character.ascender));
        }
        return max.x > min.x && max.y > min.y;
    }

    private static void Place(GameObject button, Vector2 min, Vector2 max, string label)
    {
        TMP_Text? text = button.GetComponentInChildren<TMP_Text>();
        if (text && text!.text != label) text.text = label;
        RectTransform rect = (RectTransform)button.transform;
        Vector2 size = max - min + ButtonPadding;
        Vector3 center = (min + max) / 2f + ButtonOffset;
        if (rect.sizeDelta != size) rect.sizeDelta = size;
        if (rect.localPosition != center) rect.localPosition = center;
        if (!button.activeSelf) button.SetActive(true);
    }

    // A copy of the crafting panel's Craft button: the game's stone button, the same look as the Compendium's Close.
    private static GameObject? CreateButton(TMP_Text area, int index)
    {
        Button? template = InventoryGui.instance ? InventoryGui.instance.m_craftButton : null;
        if (!template) return null;

        GameObject copy = UnityEngine.Object.Instantiate(template!.gameObject, area.transform);
        copy.name = "ProtectSpawnerFarms_ShowOnMap";
        foreach (UIGamePad gamePad in copy.GetComponentsInChildren<UIGamePad>(true)) UnityEngine.Object.Destroy(gamePad);
        foreach (UITooltip tooltip in copy.GetComponentsInChildren<UITooltip>(true)) UnityEngine.Object.Destroy(tooltip);

        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        TMP_Text[] labels = copy.GetComponentsInChildren<TMP_Text>(true);
        for (int label = 0; label < labels.Length; label++)
        {
            if (label > 0)
            {
                labels[label].gameObject.SetActive(false);
                continue;
            }
            labels[0].enableAutoSizing = true;
            labels[0].fontSizeMin = 10f;
            labels[0].fontSizeMax = area.fontSize;
        }

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => ShowOnMap(index));
        button.interactable = true;
        copy.SetActive(false);
        return copy;
    }

    private static void ClearButtons()
    {
        foreach (GameObject button in Buttons)
            if (button) UnityEngine.Object.Destroy(button);
        Buttons.Clear();
    }

    // Closes the inventory now and opens the map on the next frame, once the inventory has let go of input.
    private static void ShowOnMap(int index)
    {
        if (index < 0 || index >= Targets.Count) return;
        _pending = Targets[index];
        if (InventoryGui.instance) InventoryGui.instance.Hide();
    }

    internal static void Update()
    {
        if (_pending == null || InventoryGui.IsVisible()) return;
        MapTarget target = _pending.Value;
        _pending = null;
        if (Minimap.instance && Player.m_localPlayer)
            Minimap.instance.DiscoverLocation(target.Position, Minimap.PinType.Icon3, target.Name, true);
    }

    internal static void ClearSession()
    {
        Targets.Clear();
        ClearButtons();
        _pending = null;
    }

    private static string SharingLine() => ProtectionStore.Mode switch
    {
        ShareMode.Server => Messages.CompendiumSharedByServer,
        ShareMode.Players => Messages.CompendiumSharedWithPlayers,
        ShareMode.Host when ZNet.instance && (ZNet.instance.IsDedicated() || ZNet.instance.GetPeers().Count > 0 || ZNet.m_openServer)
            => Messages.CompendiumHosted,
        _ => Messages.CompendiumLocal
    };

    private static string Who(string name) => string.IsNullOrEmpty(name)
        ? Colored(FadedColor, Messages.CompendiumUnknownPlayer)
        : Colored(NameColor, name);

    private static string When(long ticks) => ticks <= 0
        ? Colored(FadedColor, Messages.CompendiumUnknownTime)
        : Colored(DateColor, TimeAgo.Format(ticks, DateTime.UtcNow.Ticks));

    private static string Colored(string color, string text) => $"<color={color}>{text}</color>";
}
