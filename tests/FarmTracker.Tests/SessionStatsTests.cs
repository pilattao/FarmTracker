using System;
using FarmTracker.Aggregation;
using FarmTracker.Model;
using Xunit;

namespace FarmTracker.Tests;

public class SessionStatsTests
{
    private static readonly DateTime T0 = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Effective_uses_in_map_time_raw_uses_total()
    {
        // Session: 2 completed maps, 30 min in-map total, but 2h wall-clock; profit 100.
        var s = new Session { StartUtc = T0, IncomeEx = 120, CostEx = 20 };
        s.Runs.Add(new RunRecord { StartUtc = T0, EndUtc = T0.AddMinutes(15), IncomeEx = 60, CostEx = 10 });
        s.Runs.Add(new RunRecord { StartUtc = T0.AddMinutes(60), EndUtc = T0.AddMinutes(75), IncomeEx = 60, CostEx = 10 });
        var r = SessionStats.Compute(s, activeRun: null, T0.AddHours(2));
        Assert.Equal(2, r.MapCount);
        Assert.Equal(7200, r.ElapsedSeconds, 0);
        Assert.Equal(1800, r.InMapSeconds, 0);                 // 2 x 15 min
        Assert.Equal(5400, r.OutOfMapSeconds, 0);
        Assert.Equal(50, r.ProfitPerHourEx, 3);                // 100 / 2h
        Assert.Equal(200, r.EffectiveProfitPerHourEx, 3);      // 100 / 0.5h
        Assert.Equal(50, r.AvgProfitPerMapEx, 3);
    }

    [Fact]
    public void Active_run_adds_in_map_time_but_not_map_count()
    {
        var s = new Session { StartUtc = T0, IncomeEx = 40 };
        var active = new RunRecord { StartUtc = T0, IncomeEx = 40 };
        var r = SessionStats.Compute(s, active, T0.AddMinutes(30));
        Assert.Equal(0, r.MapCount);                           // open run not counted
        Assert.Equal(1800, r.InMapSeconds, 0);                 // active run contributes live
        Assert.Equal(80, r.EffectiveProfitPerHourEx, 3);       // 40 / 0.5h
    }

    [Fact]
    public void Uses_archive_end_when_set()
    {
        var s = new Session { StartUtc = T0, EndUtc = T0.AddHours(1), IncomeEx = 100 };
        s.Runs.Add(new RunRecord { StartUtc = T0, EndUtc = T0.AddHours(1), IncomeEx = 100 });
        var r = SessionStats.Compute(s, null, T0.AddHours(5));
        Assert.Equal(100, r.ProfitPerHourEx, 3);               // total frozen at +1h
        Assert.Equal(100, r.EffectiveProfitPerHourEx, 3);      // in-map == total here
    }

    [Fact]
    public void Zero_time_and_zero_maps_are_safe()
    {
        var r = SessionStats.Compute(new Session { StartUtc = T0 }, null, T0);
        Assert.Equal(0, r.ProfitPerHourEx);
        Assert.Equal(0, r.EffectiveProfitPerHourEx);
        Assert.Equal(0, r.AvgProfitPerMapEx);
    }
}
