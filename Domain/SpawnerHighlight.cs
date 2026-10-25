using System;
using System.Collections.Generic;
using JoksterCube.ProtectSpawnerFarms.Common;
using UnityEngine;
using UnityEngine.Rendering;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using static JoksterCube.ProtectSpawnerFarms.Settings.PluginConfig;
using Object = UnityEngine.Object;
using Spawner = JoksterCube.ProtectSpawnerFarms.Domain.SpawnerRegistry.Spawner;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// While turned on, paints the nearest spawner in solid, pulsing color: the one the inventory buttons act on.
// It moves on to whichever spawner is nearest as you walk, and is drawn on top of everything, so it also shows
// through walls and trees.
internal static class SpawnerHighlight
{
    private const string Feature = "Spawner highlight";
    private const CameraEvent RenderEvent = CameraEvent.AfterForwardAlpha;
    private const float PulseSpeed = 3.5f;
    private const float MinAlpha = 0.55f;
    private const float MaxAlpha = 0.95f;
    private const float RendererRefreshInterval = 1f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly List<Renderer> Renderers = [];
    private static readonly List<Renderer> Found = [];

    private static bool _enabled;
    private static Spawner? _target;
    private static CommandBuffer? _buffer;
    private static Camera? _camera;
    private static Material? _material;
    private static bool _shaderMissing;
    private static bool _hooked;
    private static float _nextRefresh;

    internal static bool IsEnabled => _enabled;

    internal static void Toggle()
    {
        _enabled = !_enabled;
        if (_enabled && !_hooked)
        {
            Camera.onPreRender += OnPreRender;
            _hooked = true;
        }
        if (!_enabled) SetTarget(null);
        else Update();
        PlayerMessages.Center(_enabled ? Messages.HighlightOn : Messages.HighlightOff);
    }

    // Follows the nearest spawner, and turns itself off when no spawner is near.
    internal static void Update()
    {
        if (!_enabled) return;
        Player player = Player.m_localPlayer;
        // Walking away from every spawner (out of Action Distance, where the spawner panel hides) turns it off.
        Spawner? nearest = player ? SpawnerRegistry.Closest(player.transform.position, ActionDistance.Value) : null;
        if (nearest == null)
        {
            Stop();
            PlayerMessages.Center(Messages.HighlightOff);
            return;
        }
        SetTarget(nearest);
    }

    internal static void Stop()
    {
        _enabled = false;
        SetTarget(null);
    }

    private static void SetTarget(Spawner? spawner)
    {
        if (spawner == _target) return;
        _target = spawner;
        _nextRefresh = 0f;
        Renderers.Clear();
        if (spawner == null) _buffer?.Clear();
    }

    internal static void Dispose()
    {
        Stop();
        if (_hooked) Camera.onPreRender -= OnPreRender;
        _hooked = false;
        if (_camera && _buffer != null) _camera!.RemoveCommandBuffer(RenderEvent, _buffer);
        _camera = null;
        _buffer?.Release();
        _buffer = null;
        if (_material) Object.Destroy(_material);
        _material = null;
    }

    private static void OnPreRender(Camera camera)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { Draw(camera); }
        catch (Exception exception)
        {
            _buffer?.Clear();
            PatchGuard.Fail(Feature, exception);
        }
    }

    private static void Draw(Camera camera)
    {
        GameCamera gameCamera = GameCamera.instance;
        if (!gameCamera || camera.gameObject != gameCamera.gameObject) return;
        if (camera != _camera)
        {
            if (_camera && _buffer != null) _camera!.RemoveCommandBuffer(RenderEvent, _buffer);
            _buffer ??= new CommandBuffer { name = "ProtectSpawnerFarms Highlight" };
            camera.AddCommandBuffer(RenderEvent, _buffer);
            _camera = camera;
        }
        _buffer!.Clear();
        if (_target == null || !_target.IsAlive || !EnsureMaterial()) return;

        if (Time.unscaledTime >= _nextRefresh) CollectRenderers(_target.Destructible.gameObject);

        float pulse = (Mathf.Sin(Time.time * PulseSpeed) + 1f) / 2f;
        Color color = HighlightColor.Value;
        color.a = Mathf.Lerp(MinAlpha, MaxAlpha, pulse);
        _material!.SetColor(ColorId, color);

        foreach (Renderer renderer in Renderers)
        {
            // Only the level of detail the game shows right now, so overlapping copies don't stack up.
            if (!renderer || !renderer.enabled || !renderer.isVisible || !renderer.gameObject.activeInHierarchy) continue;
            int subMeshes = renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh ? skinned.sharedMesh.subMeshCount
                : renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh ? filter.sharedMesh.subMeshCount : 0;
            for (int index = 0; index < subMeshes; index++) _buffer.DrawRenderer(renderer, _material, index);
        }
    }

    // The spawner's own meshes: not its particles, and not the protection ring or bubble this mod adds to it.
    private static void CollectRenderers(GameObject target)
    {
        _nextRefresh = Time.unscaledTime + RendererRefreshInterval;
        Renderers.Clear();
        target.GetComponentsInChildren(true, Found);
        foreach (Renderer renderer in Found)
            if ((renderer is MeshRenderer or SkinnedMeshRenderer) && !ProtectedMarker.IsModVisual(renderer.transform, target.transform))
                Renderers.Add(renderer);
        Found.Clear();
    }

    private static bool EnsureMaterial()
    {
        if (_material) return true;
        if (_shaderMissing) return false;
        // UI/Default ships with every Unity build, is unlit, blends by alpha and lets the depth test be turned off.
        Shader shader = Shader.Find("UI/Default");
        if (!shader)
        {
            _shaderMissing = true;
            Plugin.ModLogger.LogWarning("UI/Default shader not found, spawner highlight is disabled.");
            return false;
        }
        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _material.SetFloat("_StencilComp", (float)CompareFunction.Always);
        _material.SetFloat("_ColorMask", (float)ColorWriteMask.All);
        _material.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        return true;
    }
}
