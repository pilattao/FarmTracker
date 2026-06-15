using System;
using System.IO;
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using FarmTracker.Aggregation;
using FarmTracker.Persistence;
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
    private SessionStore _store = null!;
    private HudWindow _window = null!;

    private bool _needSeed;
    private DateTime _lastSaveUtc = DateTime.MinValue;
    private string _currentMapName = "Map";

    public override bool Initialise()
    {
        _bridge = new NinjaPricerBridge(GameController, msg => LogError($"[bridge] {msg}"));
        _reader = new InventoryReader(GameController, _bridge, msg => LogError($"[reader] {msg}"));
        _tracker = new RunTracker();
        _accumulator = new LootAccumulator();
        _store = new SessionStore(ConfigDirectory, msg => LogError($"[store] {msg}"));
        _window = new HudWindow();

        _tracker.StartSession(DateTime.UtcNow);
        _needSeed = true;

        Input.RegisterKey(Settings.ToggleWindowHotkey.Value);
        Settings.ToggleWindowHotkey.OnValueChanged += () => Input.RegisterKey(Settings.ToggleWindowHotkey.Value);
        return true;
    }

    private bool InMapNow()
    {
        var area = GameController.Area?.CurrentArea;
        return area != null && !area.IsHideout && !area.IsTown;
    }

    public override void AreaChange(AreaInstance area)
    {
        if (!Settings.Enable || area == null) return;
        var isMap = !area.IsHideout && !area.IsTown;
        _currentMapName = area.Area?.Name ?? area.Area?.Id ?? "Map";
        var now = DateTime.UtcNow;
        if (Settings.DebugLogging)
            LogMessage($"[area] name='{area.Area?.Name}' hideout={area.IsHideout} town={area.IsTown} -> isMap={isMap}");
        _tracker.OnAreaEntered(isMap, _currentMapName, now, Settings.MapCostEx.Value);
    }

    public override void Tick()
    {
        if (!Settings.Enable) return;

        if (Settings.ToggleWindowHotkey.PressedOnce())
            Settings.ShowWindow.Value = !Settings.ShowWindow.Value;

        if (!GameController.InGame) return;

        var snapshot = _reader.Snapshot();   // unfiltered

        if (_needSeed)
        {
            _accumulator.SeedBaseline(snapshot);
            _needSeed = false;
            return;
        }

        if (!_bridge.IsAvailable || !_bridge.PricesReady) return;

        var now = DateTime.UtcNow;
        var result = _accumulator.Accumulate(snapshot, InMapNow());
        // apply the min-value threshold to income/log noise without losing presence tracking (pure + tested)
        result = LootAccumulator.ApplyMinValue(result, Settings.MinItemValueEx.Value);
        _tracker.Apply(result, now);

        if ((now - _lastSaveUtc).TotalSeconds >= 10)
        {
            _store.Save(_tracker.Session);
            _lastSaveUtc = now;
        }
    }

    public override void Render()
    {
        if (!Settings.Enable || !Settings.ShowWindow) return;
        var now = DateTime.UtcNow;
        var stats = SessionStats.Compute(_tracker.Session, _tracker.CurrentRun, now);
        var show = true;
        _window.Draw(_tracker, stats, _bridge.DivinePerExalted, _bridge.IsAvailable, _bridge.PricesReady,
                     Settings.ExpandedByDefault.Value, now, onReset: () => DoReset(now), ref show);
        if (!show) Settings.ShowWindow.Value = false;
    }

    private void DoReset(DateTime now)
    {
        var archived = _tracker.Reset(now, InMapNow(), _currentMapName, Settings.MapCostEx.Value);
        _store.Save(archived);
        _store.Prune(Settings.MaxStoredSessions.Value);
        _accumulator.Reset();
        _needSeed = true;
    }
}
