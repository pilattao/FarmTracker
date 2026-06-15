using System;
using System.IO;
using System.Linq;
using FarmTracker.Model;
using FarmTracker.Persistence;
using Xunit;

namespace FarmTracker.Tests;

public class SessionStoreTests
{
    private static string TempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "fttest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    private static Session Sess(DateTime start, double income)
    {
        var s = new Session { StartUtc = start, IncomeEx = income, CostEx = 1, SpentEx = 2 };
        s.Runs.Add(new RunRecord { Index = 1, MapName = "Mesa", StartUtc = start, EndUtc = start.AddMinutes(5), IncomeEx = income, CostEx = 1, SpentEx = 2 });
        s.Loot.Add(new LootEntry { PickedUtc = start, Name = "Exalted Orb", Category = "currency", Count = 2, UnitValueEx = 1.0, MapIndex = 1, Kind = LootKind.Picked });
        s.Loot.Add(new LootEntry { PickedUtc = start, Name = "Exalted Orb", Category = "currency", Count = 1, UnitValueEx = 2.0, MapIndex = 1, Kind = LootKind.Spent });
        return s;
    }

    [Fact]
    public void Save_then_load_round_trips_session_with_runs_and_loot()
    {
        var dir = TempDir();
        var store = new SessionStore(dir, _ => { });
        var start = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);
        store.Save(Sess(start, 42));

        var loaded = store.LoadAll(50);
        var s = Assert.Single(loaded);
        Assert.Equal(42, s.IncomeEx);
        Assert.Equal(2, s.SpentEx);
        Assert.Equal("Mesa", s.Runs[0].MapName);
        Assert.Equal(2, s.Loot.Count);
        Assert.Equal(LootKind.Spent, s.Loot[1].Kind);
        Assert.Equal(42 - 1 - 2, s.ProfitEx);
    }

    [Fact]
    public void LoadAll_is_newest_first_and_prunes_to_cap()
    {
        var dir = TempDir();
        var store = new SessionStore(dir, _ => { });
        var t = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 5; i++) store.Save(Sess(t.AddHours(i), i));

        var loaded = store.LoadAll(3);
        Assert.Equal(3, loaded.Count);
        Assert.True(loaded[0].StartUtc > loaded[1].StartUtc);     // newest first
        Assert.Equal(3, Directory.GetFiles(Path.Combine(dir, "sessions"), "*.json").Length); // pruned on disk
    }

    [Fact]
    public void Corrupt_file_is_skipped_not_thrown()
    {
        var dir = TempDir();
        var sessions = Path.Combine(dir, "sessions");
        Directory.CreateDirectory(sessions);
        File.WriteAllText(Path.Combine(sessions, "20260616-090000.json"), "{ not valid json");
        var store = new SessionStore(dir, _ => { });
        store.Save(Sess(new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc), 7));

        var loaded = store.LoadAll(50);
        Assert.Single(loaded);   // corrupt one skipped, good one kept
    }
}
