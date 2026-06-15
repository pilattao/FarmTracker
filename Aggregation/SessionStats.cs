using System;
using System.Linq;
using FarmTracker.Model;

namespace FarmTracker.Aggregation;

public sealed class SessionStatsResult
{
    public double ElapsedSeconds { get; init; }
    public int MapCount { get; init; }
    public double ProfitPerHourEx { get; init; }
    public double MapsPerHour { get; init; }
    public double AvgProfitPerMapEx { get; init; }
}

public static class SessionStats
{
    public static SessionStatsResult Compute(Session s, DateTime nowUtc)
    {
        var end = s.EndUtc ?? nowUtc;
        var elapsed = Math.Max(0, (end - s.StartUtc).TotalSeconds);
        var hours = elapsed / 3600.0;
        var maps = s.Runs.Count;
        return new SessionStatsResult
        {
            ElapsedSeconds = elapsed,
            MapCount = maps,
            ProfitPerHourEx = hours > 0 ? s.ProfitEx / hours : 0,
            MapsPerHour = hours > 0 ? maps / hours : 0,
            AvgProfitPerMapEx = maps > 0 ? s.Runs.Sum(r => r.ProfitEx) / maps : 0,
        };
    }
}
