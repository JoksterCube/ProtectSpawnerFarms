using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Plugin;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using ServerSync;
using JoksterCube.ProtectSpawnerFarms.Settings;
using JoksterCube.ProtectSpawnerFarms.Domain;
using UnityEngine;

namespace JoksterCube.ProtectSpawnerFarms;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    private readonly Harmony _harmony = new(ModGUID);

    public static readonly ManualLogSource PluginLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = ModVersion
    };

    public void Awake()
    {
        PluginConfig.Build(Config, ConfigSync);

        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
    }

    public void Update() => InputManager.Update(this);

    private void OnDestroy() => Config.Save();

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            PluginLogger.LogDebug(DebugMessages.ReadConfigCalled);
            Config.Reload();
        }
        catch
        {
            PluginLogger.LogError(DebugMessages.ErrorLoadingConfig);
            PluginLogger.LogError(DebugMessages.RequestCheckConfig);
        }
    }
}