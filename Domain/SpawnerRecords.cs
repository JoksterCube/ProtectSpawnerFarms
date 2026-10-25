using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// One spawner's protection history. Shown times are real-world UTC ticks; 0 means never.
internal sealed class SpawnerRecord
{
    internal string Key { get; }
    internal bool IsProtected { get; set; }
    // When IsProtected last changed, in game time ticks (the network clock everyone in a world shares, unlike
    // computer clocks). The newest change wins when lists are merged.
    internal long Changed { get; set; }
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

    internal SpawnerRecord Copy() => new(Key)
    {
        IsProtected = IsProtected, Changed = Changed, ProtectedBy = ProtectedBy, ProtectedById = ProtectedById, ProtectedAt = ProtectedAt,
        GuardedBy = GuardedBy, LastGuarded = LastGuarded, LastBlocked = LastBlocked
    };
}

internal enum TouchKind
{
    Guarded = 0,
    Blocked = 1
}

// Protection records of one world, with no Unity or game state, so it can be unit tested. Unprotected spawners
// stay as records for a while, so that "unprotected" also reaches players who still have them as protected.
internal sealed class SpawnerRecords
{
    private const char Separator = '\t';
    private static readonly long ForgetUnprotectedAfter = TimeSpan.FromDays(30).Ticks;
    // Change times saved by early 2.0 builds were real-world ticks (year 2000 and later is above this); game time
    // ticks stay far below it. Such old change times count as "long ago", so any newer change wins.
    private const long RealWorldTicksStart = 630000000000000000L;

    private readonly Dictionary<string, SpawnerRecord> _records = new(StringComparer.Ordinal);

    internal int ProtectedCount => _records.Values.Count(record => record.IsProtected);

    internal IEnumerable<SpawnerRecord> Protected => _records.Values.Where(record => record.IsProtected);

    internal IEnumerable<SpawnerRecord> All => _records.Values;

    internal bool IsProtected(string key) => _records.TryGetValue(key, out SpawnerRecord record) && record.IsProtected;

    internal bool TryGet(string key, out SpawnerRecord record) => _records.TryGetValue(key, out record);

    internal void Clear() => _records.Clear();

    // Returns the changed record, or null when nothing changed. changedAt is game time, now is real time.
    internal SpawnerRecord? SetProtected(string key, bool isProtected, string who, string whoId, long changedAt, long now)
    {
        if (!SpawnerKey.IsValid(key)) return null;
        if (!_records.TryGetValue(key, out SpawnerRecord record))
        {
            if (!isProtected) return null;
            _records[key] = record = new SpawnerRecord(key);
        }
        else if (record.IsProtected == isProtected) return null;

        record.IsProtected = isProtected;
        record.Changed = changedAt;
        if (isProtected)
        {
            record.ProtectedBy = Clean(who);
            record.ProtectedById = Clean(whoId);
            record.ProtectedAt = now;
        }
        return record;
    }

    // Notes that a protected spawner was guarded or blocked a hit. Returns the changed record, or null.
    internal SpawnerRecord? Touch(string key, TouchKind kind, string who, long now)
    {
        if (!_records.TryGetValue(key, out SpawnerRecord record) || !record.IsProtected) return null;
        if (kind == TouchKind.Blocked)
        {
            if (now <= record.LastBlocked) return null;
            record.LastBlocked = now;
        }
        else
        {
            if (now <= record.LastGuarded) return null;
            record.LastGuarded = now;
            record.GuardedBy = Clean(who);
        }
        return record;
    }

    // Takes in a record from someone else: the newer protection state wins, and the latest guarded and
    // blocked times are kept. Returns true when anything changed.
    internal bool Merge(SpawnerRecord incoming)
    {
        if (!SpawnerKey.IsValid(incoming.Key)) return false;
        if (!_records.TryGetValue(incoming.Key, out SpawnerRecord record))
        {
            _records[incoming.Key] = incoming.Copy();
            return true;
        }

        bool changed = false;
        if (incoming.Changed > record.Changed)
        {
            record.IsProtected = incoming.IsProtected;
            record.Changed = incoming.Changed;
            record.ProtectedBy = incoming.ProtectedBy;
            record.ProtectedById = incoming.ProtectedById;
            record.ProtectedAt = incoming.ProtectedAt;
            changed = true;
        }
        if (incoming.LastGuarded > record.LastGuarded)
        {
            record.LastGuarded = incoming.LastGuarded;
            record.GuardedBy = incoming.GuardedBy;
            changed = true;
        }
        if (incoming.LastBlocked > record.LastBlocked)
        {
            record.LastBlocked = incoming.LastBlocked;
            changed = true;
        }
        return changed;
    }

    // Replaces everything. Lines that are empty, comments or not valid are skipped. A line with only a key
    // (the 2.0 list format) counts as protected.
    internal void Load(IEnumerable<string> lines, long now)
    {
        _records.Clear();
        foreach (string line in lines)
        {
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
            SpawnerRecord? record = Parse(line);
            if (record == null) continue;
            // Unprotected records go once their last real-world note is old; they only exist to pass on the change.
            long lastSeen = Math.Max(record.ProtectedAt, Math.Max(record.LastGuarded, record.LastBlocked));
            if (!record.IsProtected && lastSeen > 0 && now - lastSeen > ForgetUnprotectedAfter) continue;
            _records[record.Key] = record;
        }
    }

    // Sorted by key, so saved files stay stable and readable.
    internal List<string> ToLines() =>
        _records.Values.OrderBy(record => record.Key, StringComparer.Ordinal).Select(Format).ToList();

    internal static string Format(SpawnerRecord record) => string.Join(Separator.ToString(),
        record.Key,
        record.IsProtected ? "1" : "0",
        Number(record.Changed),
        record.ProtectedBy,
        Number(record.ProtectedAt),
        record.GuardedBy,
        Number(record.LastGuarded),
        Number(record.LastBlocked),
        record.ProtectedById);

    internal static SpawnerRecord? Parse(string line)
    {
        string[] fields = line.Trim('\r', '\n').Split(Separator);
        string key = fields[0].Trim();
        if (!SpawnerKey.IsValid(key)) return null;
        SpawnerRecord record = new(key) { IsProtected = true };
        if (fields.Length < 8) return record;

        record.IsProtected = fields[1] == "1";
        record.Changed = Number(fields[2]);
        record.ProtectedBy = Clean(fields[3]);
        record.ProtectedAt = Number(fields[4]);
        record.GuardedBy = Clean(fields[5]);
        record.LastGuarded = Number(fields[6]);
        record.LastBlocked = Number(fields[7]);
        if (fields.Length > 8) record.ProtectedById = Clean(fields[8]);
        if (record.Changed >= RealWorldTicksStart) record.Changed = 0;
        return record;
    }

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static long Number(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number) ? number : 0L;

    // Player names go into tab-separated lines and rich text, so tabs, line breaks and tags are taken out.
    private static string Clean(string name) =>
        new string(name.Where(c => c != Separator && c != '\n' && c != '\r' && c != '<' && c != '>').ToArray()).Trim();
}
