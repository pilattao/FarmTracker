using System;
using FarmTracker.Tracking;
using Xunit;

namespace FarmTracker.Tests;

public class RunTrackerTests
{
    private static readonly DateTime T0 = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Entering_a_map_starts_a_run_with_default_cost()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(isMap: true, "Mesa", T0, defaultCostEx: 5);
        Assert.NotNull(t.CurrentRun);
        Assert.Equal("Mesa", t.CurrentRun!.MapName);
        Assert.Equal(5, t.CurrentRun.CostEx);
    }

    [Fact]
    public void Income_goes_to_session_and_active_run()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.AddIncome(30);
        Assert.Equal(30, t.Session.IncomeEx);
        Assert.Equal(30, t.CurrentRun!.IncomeEx);
    }

    [Fact]
    public void Leaving_to_town_closes_the_run_and_books_cost()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.AddIncome(30);
        t.OnAreaEntered(false, "Hideout", T0.AddMinutes(6), 5);  // left the map
        Assert.Null(t.CurrentRun);
        var run = Assert.Single(t.Session.Runs);
        Assert.Equal(30, run.IncomeEx);
        Assert.Equal(5, run.CostEx);
        Assert.Equal(25, run.ProfitEx);
        Assert.Equal(5, t.Session.CostEx);     // booked at close
        Assert.Equal(30, t.Session.IncomeEx);
    }

    [Fact]
    public void Income_between_maps_counts_to_session_only()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.AddIncome(10);                       // in town, no active run
        Assert.Equal(10, t.Session.IncomeEx);
        Assert.Empty(t.Session.Runs);
    }

    [Fact]
    public void Map_to_map_closes_previous_and_opens_next()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.OnAreaEntered(true, "Vaal", T0.AddMinutes(5), 5);
        Assert.Single(t.Session.Runs);
        Assert.Equal("Vaal", t.CurrentRun!.MapName);
    }

    [Fact]
    public void SetRunCost_adjusts_session_cost_for_completed_runs()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.OnAreaEntered(false, "Hideout", T0.AddMinutes(6), 5);
        var run = t.Session.Runs[0];
        t.SetRunCost(run, 8);
        Assert.Equal(8, run.CostEx);
        Assert.Equal(8, t.Session.CostEx);
    }
}
