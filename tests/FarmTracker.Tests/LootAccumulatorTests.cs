using System;
using System.Collections.Generic;
using System.Linq;
using FarmTracker.Model;
using FarmTracker.Tracking;
using Xunit;

namespace FarmTracker.Tests;

public class LootAccumulatorTests
{
    private static InventorySlotSnapshot S(long id, int size, double unit, string name = "X", string cat = "currency") =>
        new() { Id = id, Size = size, UnitValueEx = unit, Name = name, IconPath = "", Category = cat };

    // ---- income path (v1 semantics preserved) ----
    [Fact]
    public void New_items_counted_after_baseline()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(new[] { S(1, 1, 5) });
        var r = a.Accumulate(new[] { S(1, 1, 5), S(2, 3, 2) }, inMap: true);
        Assert.Equal(6, r.GainedEx);
        Assert.Equal(0, r.NewUnpriced);
        var e = Assert.Single(r.Events);
        Assert.Equal(LootKind.Picked, e.Kind);
        Assert.Equal(2, e.Id);
        Assert.Equal(3, e.Count);
    }

    [Fact]
    public void Stack_growth_counts_only_increment()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(new[] { S(1, 10, 1) });
        var r = a.Accumulate(new[] { S(1, 14, 1) }, inMap: true);
        Assert.Equal(4, r.GainedEx);
        Assert.Equal(4, Assert.Single(r.Events).Count);
    }

    [Fact]
    public void Dump_to_stash_out_of_map_does_not_double_count()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(Array.Empty<InventorySlotSnapshot>());
        Assert.Equal(20, a.Accumulate(new[] { S(7, 1, 20) }, inMap: false).GainedEx);   // picked in town
        Assert.Equal(0, a.Accumulate(Array.Empty<InventorySlotSnapshot>(), inMap: false).GainedEx); // dumped
        Assert.Equal(0, a.Accumulate(new[] { S(7, 1, 20) }, inMap: false).GainedEx);    // re-pulled: not recounted
    }

    [Fact]
    public void Unpriced_new_items_counted_in_NewUnpriced()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(Array.Empty<InventorySlotSnapshot>());
        var r = a.Accumulate(new[] { S(1, 1, 0), S(2, 1, 3) }, inMap: true);
        Assert.Equal(3, r.GainedEx);
        Assert.Equal(1, r.NewUnpriced);
    }

    // ---- consumption path (new) ----
    [Fact]
    public void In_map_partial_decrease_is_spent_and_lowers_high_water()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(Array.Empty<InventorySlotSnapshot>());
        Assert.Equal(10, a.Accumulate(new[] { S(1, 10, 1) }, inMap: true).GainedEx);
        var r = a.Accumulate(new[] { S(1, 4, 1) }, inMap: true);      // spent 6 on map
        Assert.Equal(6, r.SpentEx);
        var e = Assert.Single(r.Events);
        Assert.Equal(LootKind.Spent, e.Kind);
        Assert.Equal(6, e.Count);
        // later regrow above the lowered high-water (4) counts fresh
        Assert.Equal(2, a.Accumulate(new[] { S(1, 6, 1) }, inMap: true).GainedEx);
    }

    [Fact]
    public void In_map_full_consumption_emits_spent_for_remaining_stack()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(Array.Empty<InventorySlotSnapshot>());
        Assert.Equal(10, a.Accumulate(new[] { S(9, 1, 10, "Exalted Orb") }, inMap: true).GainedEx); // picked 1 exalt
        var r = a.Accumulate(Array.Empty<InventorySlotSnapshot>(), inMap: true);   // used it on a strongbox -> gone
        Assert.Equal(10, r.SpentEx);
        var e = Assert.Single(r.Events);
        Assert.Equal(LootKind.Spent, e.Kind);
        Assert.Equal("Exalted Orb", e.Name);
        Assert.Equal(1, e.Count);
        // re-pick the same id on the map afterwards counts fresh (id was removed)
        Assert.Equal(10, a.Accumulate(new[] { S(9, 1, 10, "Exalted Orb") }, inMap: true).GainedEx);
    }

    [Fact]
    public void Out_of_map_decrease_or_disappearance_is_ignored()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(Array.Empty<InventorySlotSnapshot>());
        a.Accumulate(new[] { S(1, 10, 1) }, inMap: false);            // picked in town
        var dec = a.Accumulate(new[] { S(1, 4, 1) }, inMap: false);  // moved/dumped -> ignored
        Assert.Equal(0, dec.SpentEx);
        Assert.Empty(dec.Events);
        var gone = a.Accumulate(Array.Empty<InventorySlotSnapshot>(), inMap: false); // fully dumped
        Assert.Equal(0, gone.SpentEx);
        Assert.Empty(gone.Events);
        Assert.Equal(0, a.Accumulate(new[] { S(1, 10, 1) }, inMap: false).GainedEx); // re-pull not recounted
    }

    [Fact]
    public void Unpriced_spend_contributes_zero_value_but_removes_id()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(new[] { S(1, 1, 0, "Scroll", "currency") });   // baseline unpriced item
        var r = a.Accumulate(Array.Empty<InventorySlotSnapshot>(), inMap: true); // consumed on map
        Assert.Equal(0, r.SpentEx);
        Assert.Equal(LootKind.Spent, Assert.Single(r.Events).Kind);
        Assert.Equal(0, r.NewUnpriced);   // Spent never touches NewUnpriced
    }

    // ---- min-value filter (pure helper applied by the orchestrator) ----
    [Fact]
    public void ApplyMinValue_drops_subthreshold_pickups_but_keeps_spends()
    {
        var r = new LootResult
        {
            GainedEx = 5, SpentEx = 9, NewUnpriced = 0,
            Events = new List<LootEvent>
            {
                new() { Count = 1, UnitValueEx = 0.5, Kind = LootKind.Picked, Name = "Wisdom" }, // below 1 -> drop
                new() { Count = 1, UnitValueEx = 4.5, Kind = LootKind.Picked, Name = "Alch" },    // >= 1 -> keep
                new() { Count = 1, UnitValueEx = 9.0, Kind = LootKind.Spent,  Name = "Exalt" },    // spend -> keep
            }
        };
        var f = LootAccumulator.ApplyMinValue(r, 1.0);
        Assert.Equal(4.5, f.GainedEx);
        Assert.Equal(9, f.SpentEx);
        Assert.Equal(2, f.Events.Count);
    }

    [Fact]
    public void ApplyMinValue_is_identity_at_zero_threshold()
    {
        var r = new LootResult { GainedEx = 3, Events = new List<LootEvent> { new() { Count = 1, UnitValueEx = 0.1, Kind = LootKind.Picked } } };
        Assert.Same(r, LootAccumulator.ApplyMinValue(r, 0));
    }
}
