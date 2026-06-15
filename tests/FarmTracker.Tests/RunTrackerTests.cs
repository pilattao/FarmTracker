using System;
using System.Collections.Generic;
using FarmTracker.Model;
using FarmTracker.Tracking;
using Xunit;

namespace FarmTracker.Tests;

public class RunTrackerTests
{
    private static readonly DateTime T0 = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

    private static LootResult Picked(double ex) => new()
    { GainedEx = ex, Events = new List<LootEvent> { new() { Count = 1, UnitValueEx = ex, Kind = LootKind.Picked, Name = "X" } } };
    private static LootResult Spent(double ex) => new()
    { SpentEx = ex, Events = new List<LootEvent> { new() { Count = 1, UnitValueEx = ex, Kind = LootKind.Spent, Name = "X" } } };

    [Fact]
    public void Entering_map_opens_run_and_apply_routes_income_to_run_and_session()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, defaultCostEx: 5);
        t.Apply(Picked(30), T0);
        Assert.Equal(30, t.Session.IncomeEx);
        Assert.Equal(30, t.CurrentRun!.IncomeEx);
        Assert.Equal("Mesa", t.CurrentRun.MapName);
        Assert.Equal(5, t.CurrentRun.CostEx);
        var entry = Assert.Single(t.Session.Loot);
        Assert.Equal(1, entry.MapIndex);
        Assert.Equal(LootKind.Picked, entry.Kind);
    }

    [Fact]
    public void In_map_spend_accrues_live_to_run_and_session_and_is_not_resummed_on_close()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.Apply(Picked(30), T0);
        t.Apply(Spent(8), T0);                        // used currency on the map
        Assert.Equal(8, t.CurrentRun!.SpentEx);
        Assert.Equal(8, t.Session.SpentEx);
        t.OnAreaEntered(false, "Hideout", T0.AddMinutes(6), 5);   // close run
        Assert.Equal(8, t.Session.SpentEx);           // NOT doubled at close
        var run = Assert.Single(t.Session.Runs);
        Assert.Equal(5, run.CostEx);                  // base cost booked once
        Assert.Equal(8, run.SpentEx);
        Assert.Equal(30 - 5 - 8, run.ProfitEx);
        Assert.Equal(30 - 5 - 8, t.Session.ProfitEx);
    }

    [Fact]
    public void Income_between_maps_goes_to_session_only_and_tags_null_map()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.Apply(Picked(10), T0);                      // in town, no run
        Assert.Equal(10, t.Session.IncomeEx);
        Assert.Null(Assert.Single(t.Session.Loot).MapIndex);
        Assert.Empty(t.Session.Runs);
    }

    [Fact]
    public void Reset_archives_current_starts_new_and_reopens_run_when_in_map()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        t.OnAreaEntered(true, "Mesa", T0, 5);
        t.Apply(Picked(30), T0);
        var archived = t.Reset(T0.AddMinutes(10), inMapNow: true, mapName: "Mesa", defaultCostEx: 5);
        // archived session captured the old run + income
        Assert.Equal(30, archived.IncomeEx);
        Assert.Equal(T0.AddMinutes(10), archived.EndUtc);
        Assert.Single(archived.Runs);
        // fresh session, empty, with a run re-opened for the current map
        Assert.Equal(0, t.Session.IncomeEx);
        Assert.Empty(t.Session.Loot);
        Assert.NotNull(t.CurrentRun);
        Assert.Equal("Mesa", t.CurrentRun!.MapName);
    }

    [Fact]
    public void Reset_in_town_starts_new_session_with_no_run()
    {
        var t = new RunTracker();
        t.StartSession(T0);
        var archived = t.Reset(T0.AddMinutes(5), inMapNow: false, mapName: "", defaultCostEx: 5);
        Assert.Equal(T0.AddMinutes(5), archived.EndUtc);
        Assert.Null(t.CurrentRun);
        Assert.Equal(T0.AddMinutes(5), t.Session.StartUtc);
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
