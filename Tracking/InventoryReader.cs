using System;
using System.Collections.Generic;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using FarmTracker.Pricing;

namespace FarmTracker.Tracking;

/// <summary>Builds a per-tick inventory snapshot (id, stack size, unit exalted value) from the player
/// inventory, applying a minimum-value filter. ExileCore-dependent; the pure accumulator consumes it.</summary>
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

    public IReadOnlyList<InventorySlotSnapshot> Snapshot(float minValueEx)
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
                if (stackValue < minValueEx) continue;   // below-threshold noise (e.g. scrolls)

                result.Add(new InventorySlotSnapshot
                {
                    Id = e.Id,
                    Size = size,
                    UnitValueEx = size > 0 ? stackValue / size : stackValue,
                });
            }
        }
        catch (Exception ex)
        {
            _logError($"inventory read error: {ex.Message}");
        }
        return result;
    }
}
