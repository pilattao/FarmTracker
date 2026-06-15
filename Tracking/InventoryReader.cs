using System;
using System.Collections.Generic;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Enums;          // ItemRarity
using FarmTracker.Pricing;

namespace FarmTracker.Tracking;

/// <summary>Builds the per-tick inventory snapshot (id, stack size, unit value, name, icon path,
/// category) from the player inventory. The snapshot is UNFILTERED (all ids) so the accumulator can
/// detect consumption/disappearance; the min-value threshold is applied later, in the accumulator's
/// Picked path / the UI. ExileCore-dependent.</summary>
public sealed class InventoryReader
{
    private readonly GameController _gc;
    private readonly NinjaPricerBridge _bridge;
    private readonly Action<string> _logError;

    public InventoryReader(GameController gc, NinjaPricerBridge bridge, Action<string> logError)
    {
        _gc = gc;
        _bridge = bridge;
        _logError = logError;
    }

    public IReadOnlyList<InventorySlotSnapshot> Snapshot()
    {
        var result = new List<InventorySlotSnapshot>();
        try
        {
            var inv = _gc.IngameState.ServerData.PlayerInventories[0]?.Inventory;
            var items = inv?.InventorySlotItems;
            if (items == null) return result;

            foreach (var slot in items)
            {
                var e = slot?.Item;
                if (e == null || !e.IsValid) continue;

                var size = e.TryGetComponent<Stack>(out var st) ? Math.Max(1, st.Size) : 1;
                var stackValue = _bridge.ExaltedValueOfStack(e);

                result.Add(new InventorySlotSnapshot
                {
                    Id = e.Id,
                    Size = size,
                    UnitValueEx = size > 0 ? stackValue / size : stackValue,
                    Name = ResolveName(e),
                    IconPath = e.TryGetComponent<RenderItem>(out var ri) ? (ri.ResourcePath ?? "") : "",
                    Category = ResolveCategory(e),
                });
            }
        }
        catch (Exception ex)
        {
            _logError($"inventory read error: {ex.Message}");
        }
        return result;
    }

    private string ResolveName(ExileCore2.PoEMemory.MemoryObjects.Entity e)
    {
        try
        {
            var bit = _gc.Files.BaseItemTypes.Translate(e.Path);
            return bit?.BaseName ?? "";
        }
        catch { return ""; }
    }

    private string ResolveCategory(ExileCore2.PoEMemory.MemoryObjects.Entity e)
    {
        try
        {
            if (e.TryGetComponent<Mods>(out var mods))
            {
                switch (mods.ItemRarity)
                {
                    case ItemRarity.Unique: return "unique";
                    case ItemRarity.Rare:   return "rare";
                    case ItemRarity.Magic:  return "magic";
                }
            }
            var cls = _gc.Files.BaseItemTypes.Translate(e.Path)?.ClassName ?? "";
            if (cls.Contains("Currency")) return "currency";
            if (cls.Contains("Map")) return "map";
            if (cls.Contains("Gem")) return "gem";
            return "currency";   // stackable drops default; tuned in-game
        }
        catch { return ""; }
    }
}
