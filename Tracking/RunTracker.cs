using System;
using System.Collections.Generic;
using FarmTracker.Model;

namespace FarmTracker.Tracking;

/// <summary>Pure run/session lifecycle. The plugin feeds it area-entered events and income deltas;
/// it owns the Session and the active RunRecord. No ExileCore or wall-clock inside — `now` is injected.</summary>
public sealed class RunTracker
{
    public Session Session { get; private set; } = new();
    public RunRecord? CurrentRun { get; private set; }

    public void StartSession(DateTime nowUtc)
    {
        Session = new Session { StartUtc = nowUtc };
        CurrentRun = null;
    }

    public void StopSession(DateTime nowUtc)
    {
        if (CurrentRun != null) CloseRun(nowUtc);
        Session.EndUtc = nowUtc;
    }

    /// <summary>Called on every area change. Closes the current run (if any) and starts a new one when
    /// the entered area is a map.</summary>
    public void OnAreaEntered(bool isMap, string mapName, DateTime nowUtc, double defaultCostEx)
    {
        if (CurrentRun != null) CloseRun(nowUtc);
        if (isMap)
        {
            CurrentRun = new RunRecord
            {
                Index = Session.Runs.Count + 1,
                MapName = mapName ?? "",
                StartUtc = nowUtc,
                CostEx = defaultCostEx,
            };
        }
    }

    public void AddIncome(double ex)
    {
        if (ex <= 0) return;
        Session.IncomeEx += ex;
        if (CurrentRun != null) CurrentRun.IncomeEx += ex;
    }

    public void AddUnpriced(int n)
    {
        if (n > 0) Session.UnpricedPickups += n;
    }

    /// <summary>Edit a run's cost (current or completed), keeping the session cost total consistent.</summary>
    public void SetRunCost(RunRecord run, double newCost)
    {
        if (run == null) return;
        if (Session.Runs.Contains(run)) Session.CostEx += newCost - run.CostEx;
        run.CostEx = newCost;
    }

    private void CloseRun(DateTime nowUtc)
    {
        CurrentRun!.EndUtc = nowUtc;
        Session.Runs.Add(CurrentRun);
        Session.CostEx += CurrentRun.CostEx;
        CurrentRun = null;
    }
}
