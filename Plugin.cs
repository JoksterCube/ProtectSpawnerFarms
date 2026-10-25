using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using JoksterCube.ProtectSpawnerFarms.Domain;
using JoksterCube.ProtectSpawnerFarms.Settings;
using ServerSync;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Plugin;

namespace JoksterCube.ProtectSpawnerFarms;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal static readonly ManualLogSource ModLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    internal static string ConnectionError = string.Empty;

    // ModRequired stays false: players without the mod may join a modded server, and a modded player
    // may join an unmodded server (local settings and a local protected list apply). When both have it,
    // versions must match exactly.
    // Always locked: synced settings come only from the server's config file (see EnforceServerConfigPatch).
    internal static readonly ConfigSync ServerConfigSync = new(ModGUID)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = ModVersion,
        IsLocked = true
    };

    private readonly string _configFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

    // The config file is checked for changes by a watcher and, as a fallback, this often.
    private const float ConfigCheckInterval = 2f;

    private FileSystemWatcher? _configWatcher;
    private DateTime _lastConfigWrite;
    private long _lastConfigSize = -1;
    private float _nextConfigCheck;

    private readonly Harmony _harmony = new(ModGUID);

    private void Awake()
    {
        PluginConfig.Build(Config, ServerConfigSync);

        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        RememberConfigFile();
        SetupWatcher();
    }

    private void Update()
    {
        if (Time.unscaledTime >= _nextConfigCheck)
        {
            _nextConfigCheck = Time.unscaledTime + ConfigCheckInterval;
            ReloadConfigIfChanged();
        }
        ServerGuard.Update();
        SpawnerProtection.Update();
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
        _configWatcher?.Dispose();
        SessionState.Clear();
        ProtectedMarker.Dispose();
        SpawnerHighlight.Dispose();
        // No Config.Save() here: BepInEx already saves whenever a setting changes in game, and saving on shutdown
        // would write old values over edits made to the file while the game was running.
    }

    // Edits to the config file apply in game without a restart: changed values update right away, Configuration
    // Manager shows them, and on a server they are sent to the players. Many editors (and tools like sed) save by
    // writing a new file and renaming it over the old one, which a watcher limited to one file name can miss, so
    // the whole folder is watched and any event only means "check our file". Some systems don't report file
    // changes reliably at all, so the file is also checked every few seconds.
    private void SetupWatcher()
    {
        try
        {
            var watcher = new FileSystemWatcher(Paths.ConfigPath, "*.cfg")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime
            };
            watcher.Changed += OnConfigFolderChanged;
            watcher.Created += OnConfigFolderChanged;
            watcher.Renamed += OnConfigFolderChanged;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            _configWatcher = watcher;
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception exception)
        {
            ModLogger.LogWarning($"Config file watcher unavailable, checking {ConfigFileName} every {ConfigCheckInterval} s instead: {exception.Message}");
        }
    }

    private void OnConfigFolderChanged(object sender, FileSystemEventArgs e) => ReloadConfigIfChanged();

    private void RememberConfigFile()
    {
        try
        {
            FileInfo file = new(_configFileFullPath);
            if (!file.Exists) return;
            _lastConfigWrite = file.LastWriteTimeUtc;
            _lastConfigSize = file.Length;
        }
        catch (IOException) { }
    }

    // Reloads once per actual change of the file: watchers often report a save several times.
    private void ReloadConfigIfChanged()
    {
        FileInfo file;
        try
        {
            file = new FileInfo(_configFileFullPath);
            if (!file.Exists || (file.LastWriteTimeUtc == _lastConfigWrite && file.Length == _lastConfigSize)) return;
        }
        catch (IOException) { return; }

        _lastConfigWrite = file.LastWriteTimeUtc;
        _lastConfigSize = file.Length;
        try
        {
            ModLogger.LogDebug(DebugMessages.ReadConfigCalled);
            Config.Reload();
            ModLogger.LogInfo($"{ConfigFileName} changed, settings reloaded.");
        }
        catch
        {
            // Possibly read while still being written: try again on the next check.
            _lastConfigWrite = default;
            _lastConfigSize = -1;
            ModLogger.LogError(DebugMessages.ErrorLoadingConfig);
            ModLogger.LogError(DebugMessages.RequestCheckConfig);
        }
    }
}
