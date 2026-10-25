using System;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// How long ago a moment was, the way people say it: "just now", "15 minutes ago", "2 hours ago", "3 years ago".
internal static class TimeAgo
{
    internal static string Format(long ticks, long nowTicks)
    {
        // A clock a little ahead on another computer reads as just now rather than in the future.
        TimeSpan elapsed = TimeSpan.FromTicks(Math.Max(0L, nowTicks - ticks));
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalHours < 1) return Ago((int)elapsed.TotalMinutes, "minute");
        if (elapsed.TotalDays < 1) return Ago((int)elapsed.TotalHours, "hour");
        int days = (int)elapsed.TotalDays;
        if (days < 30) return Ago(days, "day");
        if (days < 365) return Ago(days / 30, "month");
        return Ago(days / 365, "year");
    }

    private static string Ago(int count, string unit) => $"{count} {unit}{(count == 1 ? "" : "s")} ago";
}
