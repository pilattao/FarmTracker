using System;
using FarmTracker.Aggregation;
using FarmTracker.Model;
using Xunit;

namespace FarmTracker.Tests;

public class SessionStatsTests
{
    private static readonly DateTime T0 = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Rates_use_elapsed_hours()
    {
        var s = new Session { StartUtc = T0, IncomeEx = 120, CostEx = 20 };
        s.Runs.Add(new RunRecord { IncomeEx = 60, CostEx = 10 });
        s.Runs.Add(new RunRecord { IncomeEx = 60, CostEx = 10 });
        var r = SessionStats.Compute(s, T0.AddHours(2));   // 2h, profit 100, 2 maps
        Assert.Equal(2, r.MapCount);
        Assert.Equal(50, r.ProfitPerHourEx, 3);
        Assert.Equal(1, r.MapsPerHour, 3);
        Assert.Equal(50, r.AvgProfitPerMapEx, 3);
        Assert.Equal(7200, r.ElapsedSeconds, 0);
    }

    [Fact]
    public void Uses_session_end_when_stopped()
    {
        var s = new Session { StartUtc = T0, EndUtc = T0.AddHours(1), IncomeEx = 100, CostEx = 0 };
        var r = SessionStats.Compute(s, T0.AddHours(5));   // now is later, but session ended at +1h
        Assert.Equal(100, r.ProfitPerHourEx, 3);
    }

    [Fact]
    public void Zero_elapsed_and_zero_maps_are_safe()
    {
        var r = SessionStats.Compute(new Session { StartUtc = T0 }, T0);
        Assert.Equal(0, r.ProfitPerHourEx);
        Assert.Equal(0, r.MapsPerHour);
        Assert.Equal(0, r.AvgProfitPerMapEx);
    }
}
