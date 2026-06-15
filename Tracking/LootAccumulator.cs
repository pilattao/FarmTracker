using System.Collections.Generic;

namespace FarmTracker.Tracking;

public sealed class InventorySlotSnapshot
{
    public long Id { get; set; }
    public int Size { get; set; }
    public double UnitValueEx { get; set; }   // exalted value of one unit of this item
}

public readonly struct LootDelta
{
    public LootDelta(double gainedEx, int newUnpriced) { GainedEx = gainedEx; NewUnpriced = newUnpriced; }
    public double GainedEx { get; }
    public int NewUnpriced { get; }
}

/// <summary>Accumulates collected loot from per-tick inventory snapshots: new entity ids and stack
/// growth count once; dumping/re-pulling never double-counts (high-water mark per id).
/// Known tradeoff: the high-water mark is per id, so if a stack is spent down and later regrown under
/// the same id, only growth above the previous high is counted — this biases income slightly downward
/// but prevents the far worse double-counting when loot is dumped to stash and re-pulled.</summary>
public sealed class LootAccumulator
{
    private readonly Dictionary<long, int> _counted = new();   // id → highest counted stack size
    public bool Seeded { get; private set; }

    public void SeedBaseline(IEnumerable<InventorySlotSnapshot> snapshot)
    {
        _counted.Clear();
        foreach (var s in snapshot) _counted[s.Id] = s.Size;
        Seeded = true;
    }

    public LootDelta Accumulate(IEnumerable<InventorySlotSnapshot> snapshot)
    {
        double gained = 0;
        int unpriced = 0;
        foreach (var s in snapshot)
        {
            if (!_counted.TryGetValue(s.Id, out var prev))
            {
                gained += s.UnitValueEx * s.Size;
                if (s.UnitValueEx <= 0) unpriced++;
                _counted[s.Id] = s.Size;
            }
            else if (s.Size > prev)
            {
                gained += s.UnitValueEx * (s.Size - prev);
                _counted[s.Id] = s.Size;
            }
        }
        return new LootDelta(gained, unpriced);
    }

    public void Reset() { _counted.Clear(); Seeded = false; }
}
