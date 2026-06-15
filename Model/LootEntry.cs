using System;
using System.Text.Json.Serialization;

namespace FarmTracker.Model;

public enum LootKind { Picked, Spent }

/// <summary>One row in the pickup log: an item picked up (or consumed on a map). Count is always a
/// positive magnitude; the sign is conveyed by Kind (Spent reduces profit).</summary>
public sealed class LootEntry
{
    public DateTime PickedUtc { get; set; }
    public string Name { get; set; } = "";
    public string IconPath { get; set; } = "";   // captured for a future real-icon pass; unused in Stage 1
    public string Category { get; set; } = "";    // coarse class/rarity key for the dot color
    public int Count { get; set; }
    public double UnitValueEx { get; set; }
    public int? MapIndex { get; set; }            // RunRecord.Index during a run; null between maps
    public LootKind Kind { get; set; }

    [JsonIgnore] public double TotalValueEx => Count * UnitValueEx;
}
