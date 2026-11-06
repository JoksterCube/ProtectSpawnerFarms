using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// One protected spawner as the Compendium shows it: a copy of what is saved on the spawner (see SpawnerData).
// Shown times are real-world UTC ticks; 0 means never.
internal sealed class SpawnerRecord
{
    internal string Key { get; }
    // Only false in a change sent to players, saying the spawner is no longer protected.
    internal bool IsProtected { get; set; } = true;
    internal string ProtectedBy { get; set; } = string.Empty;
    // The protector's player id (Steam id or similar), for "only the protector can unprotect".
    internal string ProtectedById { get; set; } = string.Empty;
    internal long ProtectedAt { get; set; }
    // Last time a player with the mod was near and kept it safe.
    internal string GuardedBy { get; set; } = string.Empty;
    internal long LastGuarded { get; set; }
    // Last time a hit on it was blocked.
    internal long LastBlocked { get; set; }

    internal SpawnerRecord(string key) => Key = key;

    internal bool SameAs(SpawnerRecord other) =>
        Key == other.Key && IsProtected == other.IsProtected && ProtectedBy == other.ProtectedBy && ProtectedById == other.ProtectedById
        && ProtectedAt == other.ProtectedAt && GuardedBy == other.GuardedBy && LastGuarded == other.LastGuarded && LastBlocked == other.LastBlocked;
}

internal enum TouchKind
{
    Guarded = 0,
    Blocked = 1
}

// The protected spawners this game knows of, for the Compendium. No Unity or game state, so it can be unit tested.
internal sealed class SpawnerRecords
{
    private const char Separator = '\t';

    private readonly Dictionary<string, SpawnerRecord> _records = new(StringComparer.Ordinal);

    internal int Count => _records.Count;

    internal IEnumerable<SpawnerRecord> All => _records.Values;

    internal bool TryGet(string key, out SpawnerRecord record) => _records.TryGetValue(key, out record);

    internal void Clear() => _records.Clear();

    // Adds or replaces a protected spawner, or takes out one that is no longer protected.
    // Returns true when the list changed.
    internal bool Set(SpawnerRecord record)
    {
        if (!SpawnerKey.IsValid(record.Key)) return false;
        if (!record.IsProtected) return _records.Remove(record.Key);
        if (_records.TryGetValue(record.Key, out SpawnerRecord known) && known.SameAs(record)) return false;
        _records[record.Key] = record;
        return true;
    }

    // One record per line, for sending to players.
    internal static string Format(SpawnerRecord record) => string.Join(Separator.ToString(),
        record.Key,
        record.IsProtected ? "1" : "0",
        record.ProtectedBy,
        record.ProtectedById,
        Number(record.ProtectedAt),
        record.GuardedBy,
        Number(record.LastGuarded),
        Number(record.LastBlocked));

    internal static SpawnerRecord? Parse(string line)
    {
        string[] fields = line.Trim('\r', '\n').Split(Separator);
        if (fields.Length < 8 || !SpawnerKey.IsValid(fields[0])) return null;
        return new SpawnerRecord(fields[0])
        {
            IsProtected = fields[1] == "1",
            ProtectedBy = Clean(fields[2]),
            ProtectedById = Clean(fields[3]),
            ProtectedAt = Number(fields[4]),
            GuardedBy = Clean(fields[5]),
            LastGuarded = Number(fields[6]),
            LastBlocked = Number(fields[7])
        };
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static long Number(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number) ? number : 0L;

    // Player names go into tab-separated lines and rich text, so tabs, line breaks and tags are taken out.
    internal static string Clean(string? name) => name == null
        ? string.Empty
        : new string(name.Where(c => c != Separator && c != '\n' && c != '\r' && c != '<' && c != '>').ToArray()).Trim();
}
