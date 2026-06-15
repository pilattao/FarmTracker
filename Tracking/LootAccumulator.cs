using System.Collections.Generic;
using FarmTracker.Model;

namespace FarmTracker.Tracking;

public sealed class InventorySlotSnapshot
{
    public long Id { get; set; }
    public int Size { get; set; }
    public double UnitValueEx { get; set; }
    public string Name { get; set; } = "";
    public string IconPath { get; set; } = "";
    public string Category { get; set; } = "";
}

public sealed class LootEvent
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string IconPath { get; set; } = "";
    public string Category { get; set; } = "";
    public int Count { get; set; }
    public double UnitValueEx { get; set; }
    public LootKind Kind { get; set; }
}

public sealed class LootResult
{
    public double GainedEx { get; set; }
    public double SpentEx { get; set; }
    public int NewUnpriced { get; set; }
    public IReadOnlyList<LootEvent> Events { get; set; } = new List<LootEvent>();
}

/// <summary>Accumulates collected loot AND in-map consumption from per-tick UNFILTERED inventory
/// snapshots. New ids + stack growth = Picked income (high-water mark, so dump/re-pull never
/// double-counts). A stack decrease — including dropping to zero / leaving the inventory — while in a
/// map = Spent (consumed on the map): it lowers/clears the high-water mark and adds to the run cost.
/// A decrease outside a map is ignored (stash-dump assumption).</summary>
public sealed class LootAccumulator
{
    private sealed class Tracked
    {
        public int HighWater;
        public double UnitValueEx;
        public string Name = "";
        public string IconPath = "";
        public string Category = "";
    }

    private readonly Dictionary<long, Tracked> _counted = new();
    public bool Seeded { get; private set; }

    public void SeedBaseline(IEnumerable<InventorySlotSnapshot> snapshot)
    {
        _counted.Clear();
        foreach (var s in snapshot)
            _counted[s.Id] = new Tracked { HighWater = s.Size, UnitValueEx = s.UnitValueEx, Name = s.Name, IconPath = s.IconPath, Category = s.Category };
        Seeded = true;
    }

    public LootResult Accumulate(IEnumerable<InventorySlotSnapshot> snapshot, bool inMap)
    {
        double gained = 0, spent = 0;
        int unpriced = 0;
        var events = new List<LootEvent>();
        var present = new HashSet<long>();

        foreach (var s in snapshot)
        {
            present.Add(s.Id);
            if (!_counted.TryGetValue(s.Id, out var t))
            {
                gained += s.UnitValueEx * s.Size;
                if (s.UnitValueEx <= 0) unpriced++;
                _counted[s.Id] = new Tracked { HighWater = s.Size, UnitValueEx = s.UnitValueEx, Name = s.Name, IconPath = s.IconPath, Category = s.Category };
                events.Add(Mk(s.Id, s.Name, s.IconPath, s.Category, s.Size, s.UnitValueEx, LootKind.Picked));
            }
            else if (s.Size > t.HighWater)
            {
                var inc = s.Size - t.HighWater;
                gained += s.UnitValueEx * inc;
                t.HighWater = s.Size;
                t.UnitValueEx = s.UnitValueEx;   // refresh last-known value/metadata
                t.Name = s.Name; t.IconPath = s.IconPath; t.Category = s.Category;
                events.Add(Mk(s.Id, s.Name, s.IconPath, s.Category, inc, s.UnitValueEx, LootKind.Picked));
            }
            else if (s.Size < t.HighWater && inMap)
            {
                var dec = t.HighWater - s.Size;
                spent += t.UnitValueEx * dec;
                t.HighWater = s.Size;
                events.Add(Mk(s.Id, t.Name, t.IconPath, t.Category, dec, t.UnitValueEx, LootKind.Spent));
            }
            // else: decrease while out of map, or unchanged -> nothing
        }

        if (inMap)
        {
            // Ids that vanished entirely from the inventory while on a map = fully consumed.
            var gone = new List<long>();
            foreach (var kv in _counted)
                if (!present.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone)
            {
                var t = _counted[id];
                spent += t.UnitValueEx * t.HighWater;
                events.Add(Mk(id, t.Name, t.IconPath, t.Category, t.HighWater, t.UnitValueEx, LootKind.Spent));
                _counted.Remove(id);
            }
        }

        return new LootResult { GainedEx = gained, SpentEx = spent, NewUnpriced = unpriced, Events = events };
    }

    private static LootEvent Mk(long id, string name, string icon, string cat, int count, double unit, LootKind kind) =>
        new() { Id = id, Name = name, IconPath = icon, Category = cat, Count = count, UnitValueEx = unit, Kind = kind };

    /// <summary>Apply a minimum whole-pickup value threshold: drop Picked events below the threshold
    /// from income/log; Spent events and SpentEx pass through untouched. Presence/high-water tracking
    /// already happened in Accumulate over the unfiltered set, so this only gates income + log noise.</summary>
    public static LootResult ApplyMinValue(LootResult r, double minValueEx)
    {
        if (minValueEx <= 0) return r;
        double gained = 0;
        int unpriced = 0;
        var kept = new List<LootEvent>();
        foreach (var e in r.Events)
        {
            if (e.Kind == LootKind.Picked)
            {
                if (e.UnitValueEx * e.Count < minValueEx) continue;   // sub-threshold pickup
                gained += e.UnitValueEx * e.Count;
                if (e.UnitValueEx <= 0) unpriced++;
            }
            kept.Add(e);
        }
        return new LootResult { GainedEx = gained, SpentEx = r.SpentEx, NewUnpriced = unpriced, Events = kept };
    }

    public void Reset() { _counted.Clear(); Seeded = false; }
}
