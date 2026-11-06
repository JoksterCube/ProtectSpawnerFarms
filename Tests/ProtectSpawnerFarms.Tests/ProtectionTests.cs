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

    private static SpawnerRecord Protected(string key, string by = "A") =>
        new(key) { ProtectedBy = by, ProtectedById = "id-" + by, ProtectedAt = 100 };

    [Fact]
    public void Parses_key_parts()
    {
        Assert.True(SpawnerKey.TryParse(Nest, out string prefab, out float x, out float y, out float z));
        Assert.Equal(("Spawner_GreydwarfNest", 1f, 2f, 3f), (prefab, x, y, z));
    }

    [Fact]
    public void Lists_protected_spawners()
    {
        SpawnerRecords records = new();
        Assert.True(records.Set(Protected(Nest)));
        Assert.True(records.Set(Protected(Pile)));
        Assert.Equal(2, records.Count);
        Assert.True(records.TryGet(Nest, out SpawnerRecord record));
        Assert.Equal("A", record.ProtectedBy);
    }

    [Fact]
    public void Reports_changes_only_when_something_changed()
    {
        SpawnerRecords records = new();
        Assert.True(records.Set(Protected(Nest)));
        Assert.False(records.Set(Protected(Nest)));
        SpawnerRecord guarded = Protected(Nest);
        guarded.LastGuarded = 200;
        guarded.GuardedBy = "B";
        Assert.True(records.Set(guarded));
    }

    [Fact]
    public void Unprotected_spawners_leave_the_list()
    {
        SpawnerRecords records = new();
        records.Set(Protected(Nest));
        Assert.True(records.Set(new SpawnerRecord(Nest) { IsProtected = false }));
        Assert.False(records.TryGet(Nest, out _));
        Assert.False(records.Set(new SpawnerRecord(Nest) { IsProtected = false }));
    }

    [Fact]
    public void Ignores_invalid_keys()
    {
        SpawnerRecords records = new();
        Assert.False(records.Set(Protected("not a key")));
        Assert.Equal(0, records.Count);
    }

    [Fact]
    public void Sends_and_reads_the_same()
    {
        SpawnerRecord record = new(Nest)
        {
            ProtectedBy = "Testeroo", ProtectedById = "76561198042972958", ProtectedAt = 100,
            GuardedBy = "Viking", LastGuarded = 200, LastBlocked = 300
        };
        SpawnerRecord? read = SpawnerRecords.Parse(SpawnerRecords.Format(record));
        Assert.NotNull(read);
        Assert.True(record.SameAs(read!));
    }

    [Fact]
    public void Sends_and_reads_unprotected()
    {
        SpawnerRecord? read = SpawnerRecords.Parse(SpawnerRecords.Format(new SpawnerRecord(Nest) { IsProtected = false }));
        Assert.NotNull(read);
        Assert.False(read!.IsProtected);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("Spawner_GreydwarfNest@1.0,2.0,3.0")]
    [InlineData("not a key\t1\tA\tid\t1\t\t0\t0")]
    public void Skips_bad_lines(string line) => Assert.Null(SpawnerRecords.Parse(line));

    [Fact]
    public void Player_names_cannot_break_lines_or_text() =>
        Assert.Equal("BadbName/b", SpawnerRecords.Clean("Bad\t<b>Name</b>\n"));
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
