using FarmTracker.Model;
using Xunit;

namespace FarmTracker.Tests;

public class LootEntryTests
{
    [Fact]
    public void TotalValue_is_count_times_unit_for_both_kinds()
    {
        var picked = new LootEntry { Count = 6, UnitValueEx = 1.0, Kind = LootKind.Picked };
        var spent = new LootEntry { Count = 1, UnitValueEx = 9.0, Kind = LootKind.Spent };
        Assert.Equal(6, picked.TotalValueEx);
        Assert.Equal(9, spent.TotalValueEx);   // positive magnitude; sign conveyed by Kind
    }
}
