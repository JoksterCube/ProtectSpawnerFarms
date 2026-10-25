using JoksterCube.ProtectSpawnerFarms.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using Spawner = JoksterCube.ProtectSpawnerFarms.Domain.SpawnerRegistry.Spawner;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// A small panel left of the player panel in the inventory screen, shown while a spawner is within Action Distance.
// It slides out from behind the player panel and back. Its two buttons protect or unprotect the closest spawner,
// and turn highlighting of the nearest spawner on or off. A red cross means a click turns that thing off.
internal static class SpawnerButtons
{
    private const float ButtonSize = 52f;
    private const float ButtonGap = 8f;
    private const float Padding = 12f;
    private const float PanelGap = 2f;
    private const float IconPadding = 7f;
    private const float SlideSeconds = 0.18f;
    private const float CrossThickness = 4f;
    private const float CrossInset = 13f;
    private const string FlapSprite = "woodpanel_flik";
    // The game tints its wooden panels darker than their textures.
    private static readonly Color FallbackPanelTint = new(0.45f, 0.38f, 0.34f, 1f);
    private static readonly Color CrossColor = new(0.9f, 0.15f, 0.1f, 1f);
    private static readonly Color CrossEdge = new(0.1f, 0.05f, 0.03f, 0.85f);
    private static readonly Vector3[] Corners = new Vector3[4];

    private sealed class SpawnerButton(Image icon, GameObject cross, UITooltip? tooltip)
    {
        internal Image Icon { get; } = icon;
        internal GameObject Cross { get; } = cross;
        internal UITooltip? Tooltip { get; } = tooltip;
    }

    private static RectTransform? _root;
    private static RectTransform? _anchorPanel;
    private static SpawnerButton? _protect;
    private static SpawnerButton? _highlight;
    private static Spawner? _closest;
    // 0 = tucked behind the player panel, 1 = fully out.
    private static float _shown;

    internal static void Update(InventoryGui gui)
    {
        Player player = Player.m_localPlayer;
        Spawner? closest = player && InventoryGui.IsVisible()
            ? SpawnerRegistry.Closest(player!.transform.position, PluginConfig.ActionDistance.Value)
            : null;
        if (closest != null) _closest = closest;

        if (!_root)
        {
            if (closest == null) return;
            Create(gui);
            if (!_root) return;
        }

        float target = closest != null ? 1f : 0f;
        _shown = Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime / SlideSeconds);
        bool visible = _shown > 0f;
        if (_root!.gameObject.activeSelf != visible) _root.gameObject.SetActive(visible);
        if (!visible) return;
        Place();

        // While sliding away, the buttons keep showing the spawner they were last about.
        if (_closest == null || !_closest.IsAlive) return;
        Vector3 from = player ? player!.transform.position : _closest.Position;
        string meters = Vector3.Distance(from, _closest.Position).ToString("0");

        bool isProtected = ProtectionStore.IsProtected(_closest.Key);
        SetState(_protect!, isProtected, Icons.Shield,
            isProtected ? Messages.UnprotectTooltipTopic : Messages.ProtectTooltipTopic,
            string.Format(isProtected ? Messages.UnprotectTooltip : Messages.ProtectTooltip, _closest.Name, meters));

        bool highlighting = SpawnerHighlight.IsEnabled;
        SetState(_highlight!, highlighting, Icons.Eye,
            highlighting ? Messages.StopHighlightTooltipTopic : Messages.HighlightTooltipTopic,
            string.Format(highlighting ? Messages.StopHighlightTooltip : Messages.HighlightTooltip, _closest.Name, meters));
    }

    // A cross shows what a click does: crossed while on (a click turns it off), plain while off.
    private static void SetState(SpawnerButton button, bool on, string[] iconItems, string topic, string text)
    {
        if (!button.Icon.sprite) button.Icon.sprite = Icons.Find(iconItems);
        if (button.Cross.activeSelf != on) button.Cross.SetActive(on);
        if (!button.Tooltip || (button.Tooltip!.m_topic == topic && button.Tooltip.m_text == text)) return;
        button.Tooltip.m_topic = topic;
        button.Tooltip.m_text = text;
    }

    // Lines the panel up with the player panel's left edge and middle, so it follows that panel as the inventory
    // opens. Sliding moves it from behind the player panel out to the left.
    private static void Place()
    {
        if (!_anchorPanel) return;
        _anchorPanel!.GetWorldCorners(Corners);
        Vector3 leftMiddle = (Corners[0] + Corners[1]) / 2f;
        Vector3 local = _root!.parent.InverseTransformPoint(leftMiddle);

        float eased = _shown * _shown * (3f - 2f * _shown);
        float hiddenX = local.x + _root.rect.width;
        float shownX = local.x - PanelGap;
        _root.localPosition = new Vector3(Mathf.Lerp(hiddenX, shownX, eased), local.y, 0f);
    }

    private static void OnProtect()
    {
        if (_closest != null && _closest.IsAlive) SpawnerProtection.Toggle(_closest);
    }

    private static void OnHighlight() => SpawnerHighlight.Toggle();

    private static void Create(InventoryGui gui)
    {
        _anchorPanel = PlayerPanel(gui);
        if (!_anchorPanel) return;

        // Placed just before the player panel among its siblings, so it is drawn behind it while sliding.
        GameObject rootObject = new("ProtectSpawnerFarms_Buttons", typeof(RectTransform));
        rootObject.transform.SetParent(_anchorPanel!.parent, false);
        rootObject.transform.SetSiblingIndex(_anchorPanel.GetSiblingIndex());
        _root = (RectTransform)rootObject.transform;
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(1f, 0.5f);
        _root.sizeDelta = new Vector2(ButtonSize + Padding * 2f, ButtonSize * 2f + ButtonGap + Padding * 2f);
        _root.localScale = Vector3.one;
        CreateBackground(gui, _root);

        float firstY = (ButtonSize + ButtonGap) / 2f;
        _protect = CreateButton(gui, _root, "Protect", firstY, OnProtect);
        _highlight = CreateButton(gui, _root, "Highlight", -firstY, OnHighlight);
        _shown = 0f;
        rootObject.SetActive(false);
    }

    // The panel with the player name, which the spawner panel hangs next to.
    private static RectTransform? PlayerPanel(InventoryGui gui)
    {
        if (!gui.m_playerName) return null;
        Transform current = gui.m_playerName.transform;
        while (current.parent && current.parent != gui.m_inventoryRoot) current = current.parent;
        return current.parent ? current as RectTransform : null;
    }

    // The game's side flap, the wooden board the repair button sits on: flat on one edge, rounded on the other.
    // Mirrored so the flat edge meets the player panel, and tinted like the game's own flap.
    private static void CreateBackground(InventoryGui gui, RectTransform root)
    {
        RectTransform rect = Child(root, "Background", 0f);
        rect.localScale = new Vector3(-1f, 1f, 1f);
        Image background = rect.gameObject.AddComponent<Image>();
        background.raycastTarget = true;

        Image? gameFlap = null;
        if (gui.m_repairPanel)
            foreach (Image image in gui.m_repairPanel.GetComponentsInChildren<Image>(true))
                if (image.sprite && image.sprite.name.StartsWith(FlapSprite, System.StringComparison.Ordinal))
                {
                    gameFlap = image;
                    break;
                }

        background.sprite = FindSprite(FlapSprite) ?? gameFlap?.sprite;
        background.type = background.sprite && background.sprite!.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        background.color = gameFlap ? gameFlap!.color : FallbackPanelTint;
        if (gameFlap) background.material = gameFlap!.material;
    }

    private static Sprite? FindSprite(string name)
    {
        foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            if (sprite.name == name) return sprite;
        return null;
    }

    // A copy of the crafting tab button, so it looks like the game's own; its label is swapped for an icon.
    private static SpawnerButton CreateButton(InventoryGui gui, RectTransform panel, string name, float y, UnityAction onClick)
    {
        GameObject root;
        if (gui.m_tabCraft)
        {
            root = Object.Instantiate(gui.m_tabCraft.gameObject, panel);
            foreach (Transform child in root.transform) Object.Destroy(child.gameObject);
            foreach (UIGamePad gamePad in root.GetComponentsInChildren<UIGamePad>(true)) Object.Destroy(gamePad);
        }
        else
        {
            root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(panel, false);
            root.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f, 0.9f);
        }
        root.name = name;
        root.SetActive(true);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Button button = root.GetComponent<Button>();
        if (!root.TryGetComponent(out Image background))
        {
            // The frame was on a removed child: put the same frame on the button itself.
            background = root.AddComponent<Image>();
            if (gui.m_tabCraft && gui.m_tabCraft.targetGraphic is Image frame && frame)
            {
                background.sprite = frame.sprite;
                background.type = frame.type;
                background.color = frame.color;
            }
            else background.color = new Color(0.15f, 0.12f, 0.1f, 0.9f);
        }
        button.targetGraphic = background;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
        button.interactable = true;

        Image icon = Child(root.transform, "Icon", IconPadding).gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject cross = CreateCross(root.transform);

        if (!root.TryGetComponent(out UITooltip tooltip)) tooltip = root.AddComponent<UITooltip>();
        UITooltip? source = gui.m_craftButton ? gui.m_craftButton.GetComponent<UITooltip>() : null;
        if (source && !tooltip.m_tooltipPrefab) tooltip.m_tooltipPrefab = source!.m_tooltipPrefab;

        return new SpawnerButton(icon, cross, tooltip.m_tooltipPrefab ? tooltip : null);
    }

    // A red X over the icon, thin enough to keep the icon readable, with a dark edge so it shows on any icon.
    private static GameObject CreateCross(Transform button)
    {
        RectTransform cross = Child(button, "Cross", CrossInset);
        float length = (ButtonSize - CrossInset * 2f) * 1.41f;
        foreach (float angle in new[] { 45f, -45f }) Bar(cross, angle, length + 3f, CrossThickness + 3f, CrossEdge);
        foreach (float angle in new[] { 45f, -45f }) Bar(cross, angle, length, CrossThickness, CrossColor);
        return cross.gameObject;
    }

    private static void Bar(RectTransform parent, float angle, float length, float thickness, Color color)
    {
        GameObject bar = new("Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)bar.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = bar.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static RectTransform Child(Transform parent, string name, float inset)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return rect;
    }

    // The inventory screen is rebuilt for every session, so the panel is made again next time.
    internal static void ClearSession()
    {
        if (_root) Object.Destroy(_root!.gameObject);
        _root = null;
        _anchorPanel = null;
        _protect = null;
        _highlight = null;
        _closest = null;
        _shown = 0f;
    }
}
