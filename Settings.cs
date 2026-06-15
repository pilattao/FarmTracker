using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace FarmTracker;

public class Settings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);

    [Menu("Show window", "Open/close the farm-tracker overlay.")]
    public ToggleNode ShowWindow { get; set; } = new(false);

    [Menu("Toggle window hotkey", "Press to open/close the overlay. Bind it here.")]
    public HotkeyNodeV2 ToggleWindowHotkey { get; set; } = new HotkeyNodeV2(Keys.None);

    [Menu("Expanded by default", "Show the loot log expanded when the overlay opens.")]
    public ToggleNode ExpandedByDefault { get; set; } = new(true);

    [Menu("Map cost (ex)", "Base cost subtracted from each detected map (map + scarabs/etc.).")]
    public RangeNode<float> MapCostEx { get; set; } = new(0f, 0f, 1000f);

    [Menu("Min item value to count (ex)", "Ignore picked-up items below this value in income and the log.")]
    public RangeNode<float> MinItemValueEx { get; set; } = new(0f, 0f, 100f);

    [Menu("Max stored sessions", "How many past sessions to keep on disk (older are pruned).")]
    public RangeNode<int> MaxStoredSessions { get; set; } = new(50, 1, 1000);

    [Menu("Debug logging", "Log area changes and diagnostics.")]
    public ToggleNode DebugLogging { get; set; } = new(false);
}
