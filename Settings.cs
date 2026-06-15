using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace FarmTracker;

public class Settings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);

    [Menu("Show window", "Open/close the farm-tracker window.")]
    public ToggleNode ShowWindow { get; set; } = new(false);

    [Menu("Toggle window hotkey", "Press to open/close the window. Bind it here.")]
    public HotkeyNodeV2 ToggleWindowHotkey { get; set; } = new HotkeyNodeV2(Keys.None);

    [Menu("Map cost (ex)", "Cost subtracted from each detected map (map + scarabs/etc.).")]
    public RangeNode<float> MapCostEx { get; set; } = new(0f, 0f, 1000f);

    [Menu("Min item value to count (ex)", "Ignore pickups below this value (e.g. scrolls).")]
    public RangeNode<float> MinItemValueEx { get; set; } = new(0f, 0f, 100f);

    [Menu("Auto-start session on first map", "Begin a session automatically when you enter your first map.")]
    public ToggleNode AutoStartOnFirstMap { get; set; } = new(true);

    [Menu("Debug logging", "Log area changes and a sample inventory id on tab/area change (diagnostics).")]
    public ToggleNode DebugLogging { get; set; } = new(false);
}
