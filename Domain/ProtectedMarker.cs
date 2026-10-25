using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using JoksterCube.ProtectSpawnerFarms.Common;
using Spawner = JoksterCube.ProtectSpawnerFarms.Domain.SpawnerRegistry.Spawner;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// How a protected spawner looks: the Staff of Protection's shield bubble around it, in its own color (bright blue
// by default). It is a local copy made on this game with no network object, so only players with the mod see it.
// The staff's sphere uses a distortion shader with a fixed red tint, so the sphere gets a plain see-through
// material instead: the same cloud texture, the bubble color and the chosen opacity. It turns slowly to stay alive.
internal static class ProtectedMarker
{
    private const string NamePrefix = "ProtectSpawnerFarms_";
    private const string BubblePrefab = "vfx_StaffShield";
    private const string SphereName = "Sphere";
    // The bubble's sphere is 2.5 m across at scale 1.
    private const float BubbleDiameter = 2.5f;
    private const float Padding = 0.5f;
    private const float MinRadius = 1.5f;
    private const float MaxRadius = 12f;
    private const float SpinDegreesPerSecond = 12f;

    private static bool _bubbleMissing;
    private static bool _shaderMissing;
    private static Shader? _shader;
    // One material per look, shared by all bubbles that use it.
    private static readonly System.Collections.Generic.Dictionary<(Color32 Color, int Opacity, Texture? Texture), Material> Materials = [];

    internal static void Update(Spawner spawner, bool isProtected)
    {
        bool show = isProtected && ShowShieldBubble.IsOn();
        if (spawner.Bubble && (!show || spawner.BubbleColor != ShieldBubbleColor.Value || spawner.BubbleOpacity != ShieldBubbleOpacity.Value))
        {
            Object.Destroy(spawner.Bubble);
            spawner.Bubble = null;
        }
        if (show && !spawner.Bubble) spawner.Bubble = CreateBubble(spawner);
    }

    // True for the bubble this mod hangs under a spawner.
    internal static bool IsModVisual(Transform transform, Transform spawner)
    {
        for (Transform current = transform; current && current != spawner; current = current.parent)
            if (current.name.StartsWith(NamePrefix, System.StringComparison.Ordinal)) return true;
        return false;
    }

    internal static void Dispose()
    {
        foreach (Spawner spawner in SpawnerRegistry.All)
            if (spawner.Bubble) Object.Destroy(spawner.Bubble);
        foreach (Material material in Materials.Values)
            if (material) Object.Destroy(material);
        Materials.Clear();
    }

    private static GameObject? CreateBubble(Spawner spawner)
    {
        if (_bubbleMissing || !ZNetScene.instance) return null;
        GameObject prefab = ZNetScene.instance.GetPrefab(BubblePrefab);
        if (!prefab)
        {
            _bubbleMissing = true;
            Plugin.ModLogger.LogWarning("Staff of Protection bubble not found, protected spawners get no bubble.");
            return null;
        }

        Footprint(spawner.Destructible.gameObject, out Vector3 center, out float radius);
        GameObject bubble;
        // Made like a local effect: the bubble's network object removes itself instead of joining the world.
        ZNetView.m_forceDisableInit = true;
        try { bubble = Object.Instantiate(prefab, center, Quaternion.identity); }
        finally { ZNetView.m_forceDisableInit = false; }

        bubble.name = NamePrefix + "Bubble";
        bubble.transform.SetParent(spawner.Destructible.transform, true);
        float scale = radius * 2f / BubbleDiameter;
        Vector3 parentScale = spawner.Destructible.transform.lossyScale;
        bubble.transform.localScale = new Vector3(scale / Mathf.Max(parentScale.x, 0.01f),
            scale / Mathf.Max(parentScale.y, 0.01f), scale / Mathf.Max(parentScale.z, 0.01f));

        spawner.BubbleColor = ShieldBubbleColor.Value;
        spawner.BubbleOpacity = ShieldBubbleOpacity.Value;
        Restyle(bubble, spawner.BubbleColor, spawner.BubbleOpacity);
        return bubble;
    }

    private static void Restyle(GameObject bubble, Color color, float opacity)
    {
        foreach (Renderer renderer in bubble.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name != SphereName || renderer is not MeshRenderer) continue;
            Material? material = CreateMaterial(renderer.sharedMaterial, color, opacity);
            if (material) renderer.sharedMaterial = material;
            renderer.gameObject.AddComponent<BubbleSpin>().DegreesPerSecond = SpinDegreesPerSecond;
        }
        foreach (Light light in bubble.GetComponentsInChildren<Light>(true))
            light.color = color;
    }

    // Unlit, alpha blended, drawn on both sides: the inside of the far half shows through, like a real bubble.
    private static Material? CreateMaterial(Material? original, Color color, float opacity)
    {
        if (_shaderMissing) return null;
        _shader ??= Shader.Find("UI/Default");
        if (!_shader)
        {
            _shaderMissing = true;
            Plugin.ModLogger.LogWarning("UI/Default shader not found, the shield bubble keeps the staff's look.");
            return null;
        }
        Texture? texture = original && original!.HasProperty("_MainTex") ? original.mainTexture : null;
        var key = ((Color32)color, Mathf.RoundToInt(Mathf.Clamp01(opacity) * 255f), texture);
        if (Materials.TryGetValue(key, out Material cached) && cached) return cached;

        Material material = new(_shader) { name = NamePrefix + "BubbleMaterial", hideFlags = HideFlags.DontSave };
        if (texture) material.mainTexture = texture;
        color.a = Mathf.Clamp01(opacity);
        material.color = color;
        // UI shaders take their depth test from a value the UI sets for itself; a bubble must hide behind walls.
        material.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        Materials[key] = material;
        return material;
    }

    // Center and size of the spawner: its visible model and its solid parts together, height included, so tall
    // spawners (Monument of Torment) and wide ones (Wardrobe Shaft) are both fully inside the bubble.
    private static void Footprint(GameObject spawner, out Vector3 center, out float radius)
    {
        bool found = false;
        Bounds bounds = default;
        void Add(Bounds part)
        {
            if (!found) bounds = part;
            else bounds.Encapsulate(part);
            found = true;
        }
        foreach (Renderer renderer in spawner.GetComponentsInChildren<Renderer>())
            if ((renderer is MeshRenderer or SkinnedMeshRenderer) && !IsModVisual(renderer.transform, spawner.transform))
                Add(renderer.bounds);
        foreach (Collider collider in spawner.GetComponentsInChildren<Collider>())
            if (!collider.isTrigger && !IsModVisual(collider.transform, spawner.transform))
                Add(collider.bounds);

        center = found ? bounds.center : spawner.transform.position + Vector3.up;
        float size = found ? Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) + Padding : 2f;
        radius = Mathf.Clamp(size, MinRadius, MaxRadius);
    }
}

// Slowly turns the bubble, so its clouds drift.
internal sealed class BubbleSpin : MonoBehaviour
{
    internal float DegreesPerSecond;

    private void Update() => transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, 0f, Space.World);
}
