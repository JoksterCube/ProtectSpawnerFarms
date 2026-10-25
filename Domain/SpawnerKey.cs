using System;
using System.Globalization;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// Identifies one spawner across game sessions. Object ids (ZDOID) are renumbered every time a world loads,
// but a spawner never moves, so its prefab name and position stay the same.
internal static class SpawnerKey
{
    internal static string For(string prefabName, float x, float y, float z) =>
        string.Format(CultureInfo.InvariantCulture, "{0}@{1:0.0},{2:0.0},{3:0.0}", prefabName, Round(x), Round(y), Round(z));

    // Whole tenths first, so -0.04 and 0.04 both become "0.0" and rounding never depends on culture.
    private static float Round(float value)
    {
        float rounded = (float)Math.Round(value * 10f, MidpointRounding.AwayFromZero) / 10f;
        return rounded == 0f ? 0f : rounded;
    }

    internal static bool IsValid(string key) => TryParse(key, out _, out _, out _, out _);

    internal static bool TryParse(string key, out string prefabName, out float x, out float y, out float z)
    {
        prefabName = string.Empty;
        x = y = z = 0f;
        int at = key.IndexOf('@');
        if (at <= 0) return false;
        string[] parts = key.Substring(at + 1).Split(',');
        if (parts.Length != 3
            || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
            || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
            || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            return false;
        prefabName = key.Substring(0, at);
        return true;
    }
}
