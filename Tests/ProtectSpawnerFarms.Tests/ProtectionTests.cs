using System;
using JoksterCube.ProtectSpawnerFarms.Domain;
using Xunit;

namespace JoksterCube.ProtectSpawnerFarms.Tests;

public class SpawnerKeyTests
{
    [Fact]
    public void Uses_prefab_and_position_to_one_decimal() =>
        Assert.Equal("Spawner_GreydwarfNest@12.3,45.7,-8.9", SpawnerKey.For("Spawner_GreydwarfNest", 12.34f, 45.66f, -8.94f));

    [Fact]
    public void Tiny_values_round_to_plain_zero() =>
        Assert.Equal("BonePileSpawner@0.0,0.0,0.0", SpawnerKey.For("BonePileSpawner", -0.04f, 0.04f, 0f));

    [Fact]
    public void Same_spawner_gives_same_key() =>
        Assert.Equal(SpawnerKey.For("Spawner_Hole", 1000.25f, 30f, -2000.75f), SpawnerKey.For("Spawner_Hole", 1000.25f, 30f, -2000.75f));

    [Theory]
    [InlineData("Spawner_DraugrPile@1.0,2.0,3.0", true)]
    [InlineData("Spawner_DraugrPile@1.0,2.0", false)]
    [InlineData("@1.0,2.0,3.0", false)]
    [InlineData("Spawner_DraugrPile", false)]
    [InlineData("Spawner_DraugrPile@a,2.0,3.0", false)]
    public void Validates_keys(string key, bool valid) => Assert.Equal(valid, SpawnerKey.IsValid(key));
}

public class SpawnerRecordsTests
{
    private const string Nest = "Spawner_GreydwarfNest@1.0,2.0,3.0";
    private const string Pile = "BonePileSpawner@4.0,5.0,6.0";
    private static readonly long Day = TimeSpan.FromDays(1).Ticks;

    [Fact]
    public void Parses_key_parts()
    {
        Assert.True(SpawnerKey.TryParse(Nest, out string prefab, out float x, out float y, out float z));
        Assert.Equal(("Spawner_GreydwarfNest", 1f, 2f, 3f), (prefab, x, y, z));
    }

    [Fact]
    public void Protecting_records_who_and_when()
    {
        SpawnerRecords records = new();
        SpawnerRecord? record = records.SetProtected(Nest, true, "Testeroo", "id-" + "Testeroo", 100, 100);
        Assert.NotNull(record);
        Assert.Equal("Testeroo", record!.ProtectedBy);
        Assert.Equal(100, record.ProtectedAt);
        Assert.True(records.IsProtected(Nest));
    }

    [Fact]
    public void Reports_changes_only_when_something_changed()
    {
        SpawnerRecords records = new();
        Assert.Null(records.SetProtected(Nest, false, "A", "id-" + "A", 1, 1));
        Assert.NotNull(records.SetProtected(Nest, true, "A", "id-" + "A", 2, 2));
        Assert.Null(records.SetProtected(Nest, true, "B", "id-" + "B", 3, 3));
        Assert.NotNull(records.SetProtected(Nest, false, "B", "id-" + "B", 4, 4));
        Assert.False(records.IsProtected(Nest));
    }

    [Fact]
    public void Ignores_invalid_keys()
    {
        SpawnerRecords records = new();
        Assert.Null(records.SetProtected("not a key", true, "A", "id-" + "A", 1, 1));
        Assert.Equal(0, records.ProtectedCount);
    }

    [Fact]
    public void Newest_change_wins_when_merging()
    {
        SpawnerRecords mine = new();
        mine.SetProtected(Nest, true, "A", "id-" + "A", 10, 10);

        SpawnerRecords theirs = new();
        theirs.SetProtected(Nest, true, "B", "id-" + "B", 5, 5);
        theirs.SetProtected(Nest, false, "B", "id-" + "B", 20, 20);
        theirs.TryGet(Nest, out SpawnerRecord unprotected);

        Assert.True(mine.Merge(unprotected));
        Assert.False(mine.IsProtected(Nest));

        SpawnerRecord older = new(Nest) { IsProtected = true, Changed = 15, ProtectedBy = "C" };
        Assert.False(mine.Merge(older));
        Assert.False(mine.IsProtected(Nest));
    }

    [Fact]
    public void Merging_keeps_the_latest_guarded_and_blocked_times()
    {
        SpawnerRecords records = new();
        records.SetProtected(Nest, true, "A", "id-" + "A", 10, 10);
        records.Touch(Nest, TouchKind.Guarded, "A", 50);

        SpawnerRecord incoming = new(Nest) { IsProtected = true, Changed = 10, GuardedBy = "B", LastGuarded = 40, LastBlocked = 60 };
        Assert.True(records.Merge(incoming));
        records.TryGet(Nest, out SpawnerRecord record);
        Assert.Equal((50L, "A", 60L), (record.LastGuarded, record.GuardedBy, record.LastBlocked));
    }

    [Fact]
    public void Touches_only_protected_spawners()
    {
        SpawnerRecords records = new();
        Assert.Null(records.Touch(Nest, TouchKind.Blocked, "A", 5));
        records.SetProtected(Nest, true, "A", "id-" + "A", 1, 1);
        Assert.NotNull(records.Touch(Nest, TouchKind.Blocked, "A", 5));
        Assert.Null(records.Touch(Nest, TouchKind.Blocked, "A", 5));
    }

    [Fact]
    public void Saves_and_loads_the_same()
    {
        SpawnerRecords records = new();
        records.SetProtected(Nest, true, "Testeroo", "id-" + "Testeroo", 100, 100);
        records.Touch(Nest, TouchKind.Guarded, "Viking", 200);
        records.Touch(Nest, TouchKind.Blocked, "Viking", 300);
        records.SetProtected(Pile, true, "A", "id-" + "A", 100, 100);

        SpawnerRecords reloaded = new();
        reloaded.Load(records.ToLines(), 400);
        Assert.Equal(records.ToLines(), reloaded.ToLines());
    }

    [Fact]
    public void Loads_old_key_only_lines_as_protected_and_skips_bad_lines()
    {
        SpawnerRecords records = new();
        records.Load(["# header", "", "  " + Pile + "  ", "garbage"], 0);
        Assert.Equal(1, records.ProtectedCount);
        Assert.True(records.IsProtected(Pile));
    }

    [Fact]
    public void Forgets_long_unprotected_spawners_on_load()
    {
        SpawnerRecords records = new();
        records.SetProtected(Nest, true, "A", "id-" + "A", 1, 1);
        records.SetProtected(Nest, false, "A", "id-" + "A", 2, 2);
        SpawnerRecords reloaded = new();
        reloaded.Load(records.ToLines(), 2 + 31 * Day);
        Assert.False(reloaded.TryGet(Nest, out _));
    }

    [Fact]
    public void Player_names_cannot_break_lines_or_text()
    {
        SpawnerRecords records = new();
        SpawnerRecord record = records.SetProtected(Nest, true, "Bad\t<b>Name</b>\n", "id-" + "Bad\t<b>Name</b>\n", 1, 1)!;
        Assert.Equal("BadbName/b", record.ProtectedBy);
    }
}

public class TimeAgoTests
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    private static string Ago(TimeSpan elapsed) => TimeAgo.Format((Now - elapsed).Ticks, Now.Ticks);

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(59, "just now")]
    [InlineData(60, "1 minute ago")]
    [InlineData(15 * 60, "15 minutes ago")]
    [InlineData(59 * 60 + 59, "59 minutes ago")]
    [InlineData(60 * 60, "1 hour ago")]
    [InlineData(2 * 60 * 60 + 30 * 60, "2 hours ago")]
    [InlineData(24 * 60 * 60, "1 day ago")]
    [InlineData(29 * 24 * 60 * 60, "29 days ago")]
    [InlineData(30 * 24 * 60 * 60, "1 month ago")]
    [InlineData(65 * 24 * 60 * 60, "2 months ago")]
    [InlineData(364 * 24 * 60 * 60, "12 months ago")]
    [InlineData(365 * 24 * 60 * 60, "1 year ago")]
    [InlineData(3 * 365 * 24 * 60 * 60 + 100 * 24 * 60 * 60, "3 years ago")]
    public void Says_how_long_ago(int seconds, string expected) => Assert.Equal(expected, Ago(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Future_times_are_just_now() => Assert.Equal("just now", Ago(TimeSpan.FromMinutes(-5)));
}

public class GameTimeAndProtectorTests
{
    private const string Nest = "Spawner_GreydwarfNest@1.0,2.0,3.0";

    [Fact]
    public void Keeps_the_protector_id_when_saved_and_loaded()
    {
        SpawnerRecords records = new();
        records.SetProtected(Nest, true, "Testeroo", "76561198042972958", 50, 1000);
        SpawnerRecords reloaded = new();
        reloaded.Load(records.ToLines(), 1000);
        reloaded.TryGet(Nest, out SpawnerRecord record);
        Assert.Equal(("Testeroo", "76561198042972958", 50L, 1000L), (record.ProtectedBy, record.ProtectedById, record.Changed, record.ProtectedAt));
    }

    [Fact]
    public void Change_order_uses_game_time_not_real_time()
    {
        SpawnerRecords mine = new();
        // Protected at game time 100, on a computer whose clock is far ahead.
        mine.SetProtected(Nest, true, "A", "a", 100, 900_000);
        // Unprotected later in game time (200), by someone whose clock is behind.
        SpawnerRecord unprotect = new(Nest) { IsProtected = false, Changed = 200, ProtectedAt = 10 };
        Assert.True(mine.Merge(unprotect));
        Assert.False(mine.IsProtected(Nest));
    }

    [Fact]
    public void Real_world_change_times_from_early_builds_count_as_long_ago()
    {
        string old = Nest + "\t1\t639285484181286330\tA\t639285484181286330\t\t0\t0";
        SpawnerRecord? record = SpawnerRecords.Parse(old);
        Assert.NotNull(record);
        Assert.Equal(0L, record!.Changed);
    }
}

public class PermissionTests
{
    private static ProtectionPermissions.Verdict Check(ProtectionPermission permission, bool protect, string protector, string requester, bool admin) =>
        ProtectionPermissions.Check(permission, protect, protector, requester, admin);

    [Fact]
    public void Anyone_can_change_anything() =>
        Assert.Equal(ProtectionPermissions.Verdict.Allowed, Check(ProtectionPermission.Anyone, false, "a", "b", false));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Admins_only_refuses_players(bool protect) =>
        Assert.Equal(ProtectionPermissions.Verdict.AdminsOnly, Check(ProtectionPermission.AdminsOnly, protect, "", "b", false));

    [Fact]
    public void Admins_are_always_allowed() =>
        Assert.Equal(ProtectionPermissions.Verdict.Allowed, Check(ProtectionPermission.ProtectorOrAdmin, false, "a", "b", true));

    [Fact]
    public void Anyone_can_protect_under_protector_rule() =>
        Assert.Equal(ProtectionPermissions.Verdict.Allowed, Check(ProtectionPermission.ProtectorOrAdmin, true, "", "b", false));

    [Fact]
    public void Only_the_protector_can_unprotect()
    {
        Assert.Equal(ProtectionPermissions.Verdict.Allowed, Check(ProtectionPermission.ProtectorOrAdmin, false, "a", "a", false));
        Assert.Equal(ProtectionPermissions.Verdict.ProtectorOnly, Check(ProtectionPermission.ProtectorOrAdmin, false, "a", "b", false));
    }

    [Fact]
    public void Spawners_without_a_known_protector_can_be_unprotected() =>
        Assert.Equal(ProtectionPermissions.Verdict.Allowed, Check(ProtectionPermission.ProtectorOrAdmin, false, "", "b", false));
}
