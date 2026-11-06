using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants.Plugin;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// On a server without the mod, the Compendium only learns of protected spawners you get near. This remembers them
// between sessions, one small file per server world. It is only a copy for showing: the spawner itself always
// decides, and a remembered entry is corrected as soon as you are near that spawner again.
internal static class CompendiumCache
{
    private const string FolderName = "CompendiumCache";

    private static string? _file;

    // The file for the world this game is in, or null when there is none yet.
    internal static string? FileFor()
    {
        World? world = ZNet.World;
        if (world == null || world.m_uid == 0) return null;
        string name = SafeFileName($"{world.m_name}_{world.m_uid}") + ".txt";
        return Path.Combine(Paths.ConfigPath, ModGUID, FolderName, name);
    }

    internal static List<SpawnerRecord> Load(string file)
    {
        _file = file;
        try
        {
            if (!File.Exists(file)) return [];
            List<SpawnerRecord> records = File.ReadAllLines(file)
                .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
                .Select(SpawnerRecords.Parse)
                .Where(record => record is { IsProtected: true })
                .ToList()!;
            Plugin.ModLogger.LogInfo($"Remembered {records.Count} protected spawners on this server.");
            return records;
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Could not read {file}: {exception.Message}");
            return [];
        }
    }

    internal static void Save(IEnumerable<SpawnerRecord> records)
    {
        if (_file == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            string[] header =
            [
                "# Protected spawners seen on this server, for the Compendium only. Protection itself is saved on the spawners.",
                "# key (prefab@x,y,z), protected, protected by, protector id, protected at, guarded by, last guarded, last blocked hit. Times are UTC ticks."
            ];
            File.WriteAllLines(_file, header.Concat(records.OrderBy(record => record.Key, StringComparer.Ordinal).Select(SpawnerRecords.Format)));
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Could not save {_file}: {exception.Message}");
        }
    }

    internal static void ClearSession() => _file = null;

    private static string SafeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
