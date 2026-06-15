using System;
using System.Linq;
using FarmTracker.Model;

namespace FarmTracker.Aggregation;

public sealed class SessionStatsResult
{
    public double ElapsedSeconds { get; init; }
    public double InMapSeconds { get; init; }
    public double OutOfMapSeconds { get; init; }
    public int MapCount { get; init; }
    public double ProfitPerHourEx { get; init; }
    public double EffectiveProfitPerHourEx { get; init; }
    public double AvgProfitPerMapEx { get; init; }
}

public static class SessionStats
{
    public static SessionStatsResult Compute(Session s, RunRecord? activeRun, DateTime nowUtc)
    {
        var end = s.EndUtc ?? nowUtc;
        var elapsed = Math.Max(0, (end - s.StartUtc).TotalSeconds);

        double inMap = 0;
        foreach (var r in s.Runs)
            inMap += Math.Max(0, ((r.EndUtc ?? nowUtc) - r.StartUtc).TotalSeconds);
        if (activeRun != null)
            inMap += Math.Max(0, (nowUtc - activeRun.StartUtc).TotalSeconds);
        inMap = Math.Min(inMap, elapsed);                       // never exceed wall-clock
        var outMap = Math.Max(0, elapsed - inMap);

        var totalHours = elapsed / 3600.0;
        var inMapHours = inMap / 3600.0;
        var maps = s.Runs.Count;

        return new SessionStatsResult
        {
            ElapsedSeconds = elapsed,
            InMapSeconds = inMap,
            OutOfMapSeconds = outMap,
            MapCount = maps,
            ProfitPerHourEx = totalHours > 0 ? s.ProfitEx / totalHours : 0,
            EffectiveProfitPerHourEx = inMapHours > 0 ? s.ProfitEx / inMapHours : 0,
            AvgProfitPerMapEx = maps > 0 ? s.Runs.Sum(r => r.ProfitEx) / maps : 0,
        };
    }
}
