using System;
using FarmTracker.Model;

namespace FarmTracker.Tracking;

/// <summary>Pure perpetual-session lifecycle + loot attribution. There is no Start/Stop; the session
/// always runs and Reset archives it into history and starts a fresh one. `now`/area are injected.</summary>
public sealed class RunTracker
{
    public Session Session { get; private set; } = new();
    public RunRecord? CurrentRun { get; private set; }

    public void StartSession(DateTime nowUtc)
    {
        Session = new Session { StartUtc = nowUtc };
        CurrentRun = null;
    }

    public void OnAreaEntered(bool isMap, string mapName, DateTime nowUtc, double defaultCostEx)
    {
        if (CurrentRun != null) CloseRun(nowUtc);
        if (isMap) OpenRun(mapName, nowUtc, defaultCostEx);
    }

    /// <summary>Apply one tick's loot result: route income/spend to the session (and active run) and
    /// append a LootEntry per event. SpentEx is accrued live here and must NOT be re-summed on close.</summary>
    public void Apply(LootResult result, DateTime nowUtc)
    {
        if (result.GainedEx != 0)
        {
            Session.IncomeEx += result.GainedEx;
            if (CurrentRun != null) CurrentRun.IncomeEx += result.GainedEx;
        }
        if (result.SpentEx != 0)
        {
            Session.SpentEx += result.SpentEx;
            if (CurrentRun != null) CurrentRun.SpentEx += result.SpentEx;
        }
        if (result.NewUnpriced > 0) Session.UnpricedPickups += result.NewUnpriced;

        var mapIndex = CurrentRun?.Index;
        foreach (var e in result.Events)
        {
            Session.Loot.Add(new LootEntry
            {
                PickedUtc = nowUtc,
                Name = e.Name,
                IconPath = e.IconPath,
                Category = e.Category,
                Count = e.Count,
                UnitValueEx = e.UnitValueEx,
                MapIndex = mapIndex,
                Kind = e.Kind,
            });
        }
    }

    /// <summary>Archive the current session into history and start a fresh one. Returns the archived
    /// session for the caller to persist. Re-opens a run if currently standing in a map.</summary>
    public Session Reset(DateTime nowUtc, bool inMapNow, string mapName, double defaultCostEx)
    {
        if (CurrentRun != null) CloseRun(nowUtc);
        Session.EndUtc = nowUtc;
        var archived = Session;
        Session = new Session { StartUtc = nowUtc };
        CurrentRun = null;
        if (inMapNow) OpenRun(mapName, nowUtc, defaultCostEx);
        return archived;
    }

    public void SetRunCost(RunRecord run, double newCost)
    {
        if (run == null) return;
        if (Session.Runs.Contains(run)) Session.CostEx += newCost - run.CostEx;
        run.CostEx = newCost;
    }

    private void OpenRun(string mapName, DateTime nowUtc, double defaultCostEx) =>
        CurrentRun = new RunRecord { Index = Session.Runs.Count + 1, MapName = mapName ?? "", StartUtc = nowUtc, CostEx = defaultCostEx };

    private void CloseRun(DateTime nowUtc)
    {
        CurrentRun!.EndUtc = nowUtc;
        Session.Runs.Add(CurrentRun);
        Session.CostEx += CurrentRun.CostEx;   // base cost booked once; SpentEx already live
        CurrentRun = null;
    }
}
