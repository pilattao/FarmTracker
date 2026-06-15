using System.Collections.Generic;
using FarmTracker.Tracking;
using Xunit;

namespace FarmTracker.Tests;

public class LootAccumulatorTests
{
    private static InventorySlotSnapshot S(long id, int size, double unit) =>
        new() { Id = id, Size = size, UnitValueEx = unit };

    [Fact]
    public void New_items_are_counted_after_baseline()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(new[] { S(1, 1, 5) });            // pre-existing → not income
        var d = a.Accumulate(new[] { S(1, 1, 5), S(2, 3, 2) });  // id 2 is new (3 × 2 = 6)
        Assert.Equal(6, d.GainedEx);
        Assert.Equal(0, d.NewUnpriced);
    }

    [Fact]
    public void Stack_growth_counts_only_the_increment()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(new[] { S(1, 10, 1) });
        var d = a.Accumulate(new[] { S(1, 14, 1) });     // +4
        Assert.Equal(4, d.GainedEx);
    }

    [Fact]
    public void Dump_then_repull_does_not_double_count()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(System.Array.Empty<InventorySlotSnapshot>());
        Assert.Equal(20, a.Accumulate(new[] { S(7, 1, 20) }).GainedEx);  // picked up
        Assert.Equal(0, a.Accumulate(System.Array.Empty<InventorySlotSnapshot>()).GainedEx); // dumped
        Assert.Equal(0, a.Accumulate(new[] { S(7, 1, 20) }).GainedEx);   // re-pulled: same id, not recounted
    }

    [Fact]
    public void Stack_decrease_then_regrow_counts_only_above_high_water()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(System.Array.Empty<InventorySlotSnapshot>());
        Assert.Equal(10, a.Accumulate(new[] { S(1, 10, 1) }).GainedEx);
        Assert.Equal(0, a.Accumulate(new[] { S(1, 4, 1) }).GainedEx);    // spent 6
        Assert.Equal(2, a.Accumulate(new[] { S(1, 12, 1) }).GainedEx);   // 12 > recorded 10 → +2
    }

    [Fact]
    public void Unpriced_new_items_are_counted_in_NewUnpriced()
    {
        var a = new LootAccumulator();
        a.SeedBaseline(System.Array.Empty<InventorySlotSnapshot>());
        var d = a.Accumulate(new[] { S(1, 1, 0), S(2, 1, 3) });
        Assert.Equal(3, d.GainedEx);
        Assert.Equal(1, d.NewUnpriced);
    }
}
