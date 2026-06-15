using System;
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using FarmTracker.Aggregation;
using FarmTracker.Pricing;
using FarmTracker.Tracking;
using FarmTracker.UI;

namespace FarmTracker;

public class FarmTracker : BaseSettingsPlugin<Settings>
{
    private NinjaPricerBridge _bridge = null!;
    private InventoryReader _reader = null!;
    private RunTracker _tracker = null!;
    private LootAccumulator _accumulator = null!;
    private FarmWindow _window = null!;

    private bool _sessionActive;
    private bool _needSeed;

    public override bool Initialise()
    {
        _bridge = new NinjaPricerBridge(GameController, msg => LogError($"[bridge] {msg}"));
        _reader = new InventoryReader(GameController, _bridge, msg => LogError($"[reader] {msg}"));
        _tracker = new RunTracker();
        _accumulator = new LootAccumulator();
        _window = new FarmWindow();

        Input.RegisterKey(Settings.ToggleWindowHotkey.Value);
        Settings.ToggleWindowHotkey.OnValueChanged += () => Input.RegisterKey(Settings.ToggleWindowHotkey.Value);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        if (!Settings.Enable || area == null) return;

        var isMap = !area.IsHideout && !area.IsTown;
        var mapName = area.Area?.Name ?? area.Area?.Id ?? "Map";
        var now = DateTime.UtcNow;

        if (Settings.DebugLogging)
            LogMessage($"[area] raw='{area.Area?.Id}' name='{area.Area?.Name}' hideout={area.IsHideout} town={area.IsTown} -> isMap={isMap}");

        if (!_sessionActive && isMap && Settings.AutoStartOnFirstMap.Value)
            StartSession(now);

        if (_sessionActive)
            _tracker.OnAreaEntered(isMap, mapName, now, Settings.MapCostEx.Value);
    }

    public override void Tick()
    {
        if (!Settings.Enable) return;

        if (Settings.ToggleWindowHotkey.PressedOnce())
            Settings.ShowWindow.Value = !Settings.ShowWindow.Value;

        if (!GameController.InGame || !_sessionActive) return;
        if (!_bridge.IsAvailable || !_bridge.PricesReady) return;

        var snapshot = _reader.Snapshot(Settings.MinItemValueEx.Value);
        if (_needSeed)
        {
            _accumulator.SeedBaseline(snapshot);   // existing inventory is not income
            _needSeed = false;
            return;
        }

        var delta = _accumulator.Accumulate(snapshot);
        _tracker.AddIncome(delta.GainedEx);
        _tracker.AddUnpriced(delta.NewUnpriced);
    }

    public override void Render()
    {
        if (!Settings.Enable || !Settings.ShowWindow) return;

        var now = DateTime.UtcNow;
        var stats = SessionStats.Compute(_tracker.Session, now);
        var show = true;
        _window.Draw(_tracker, stats, _bridge.DivinePerExalted, _bridge.IsAvailable, _bridge.PricesReady,
                     Settings, now, onStart: () => StartSession(now), onStop: () => StopSession(now),
                     onReset: () => StartSession(now), ref show);
        if (!show) Settings.ShowWindow.Value = false;
    }

    private void StartSession(DateTime now)
    {
        _tracker.StartSession(now);
        _accumulator.Reset();
        _needSeed = true;
        _sessionActive = true;
    }

    private void StopSession(DateTime now)
    {
        _tracker.StopSession(now);
        _sessionActive = false;
    }
}
