using System;
using System.Numerics;
using ImGuiNET;
using FarmTracker.Aggregation;
using FarmTracker.Formatting;
using FarmTracker.Model;
using FarmTracker.Tracking;

namespace FarmTracker.UI;

/// <summary>Minimal always-on overlay: a 2-line summary that expands into the live session loot log.
/// No Start/Stop — only Reset. Loot rows use a colored dot (LootDotColor) + text.</summary>
public sealed class HudWindow
{
    private bool _expanded;
    private bool _initExpand;

    public void Draw(RunTracker tracker, SessionStatsResult stats, double divinePerExalted,
                     bool bridgeAvailable, bool pricesReady, bool expandedDefault,
                     DateTime nowUtc, Action onReset, ref bool showWindow)
    {
        if (!_initExpand) { _expanded = expandedDefault; _initExpand = true; }

        Theme.Push();
        try
        {
            if (!ImGui.Begin("Farm Tracker", ref showWindow)) { ImGui.End(); return; }

            if (!bridgeAvailable)
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "NinjaPricer not loaded — income not valued.");
            else if (!pricesReady)
                ImGui.TextColored(new Vector4(1, 0.85f, 0.3f, 1), "Waiting for price data...");

            var s = tracker.Session;
            var run = tracker.CurrentRun;

            // Line 1: Reset · profit · loot/cost · caret
            if (ImGui.Button("Reset")) onReset();
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.55f, 0.85f, 1f, 1f),
                $"{CurrencyFormat.ExWithDiv(s.ProfitEx, divinePerExalted)}");
            ImGui.SameLine();
            ImGui.TextDisabled($"Loot {s.IncomeEx:0.#}  Cost {(s.CostEx + s.SpentEx):0.#}");
            ImGui.SameLine();
            if (ImGui.SmallButton(_expanded ? "v" : ">")) _expanded = !_expanded;

            // Line 2: effective · current map cost · maps · in/total time
            var mapCost = run != null ? run.CostEx + run.SpentEx : 0;
            ImGui.Text($"Eff {CurrencyFormat.ExWithDiv(stats.EffectiveProfitPerHourEx, divinePerExalted)}/hr   ·   " +
                       $"Map {mapCost:0.#} ex   ·   Maps {stats.MapCount}   ·   " +
                       $"in {Dur(stats.InMapSeconds)} / total {Dur(stats.ElapsedSeconds)}");

            if (_expanded)
            {
                ImGui.Separator();
                if (ImGui.BeginChild("ft_loot", new Vector2(0, 220), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    for (var i = s.Loot.Count - 1; i >= 0; i--)   // newest first
                    {
                        var e = s.Loot[i];
                        var col = LootDotColor.For(e.Category);
                        ImGui.TextColored(col, "●");          // ● dot
                        ImGui.SameLine();
                        if (e.Kind == LootKind.Spent)
                        {
                            ImGui.TextDisabled($"{e.Name} x{e.Count}");
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(0.9f, 0.45f, 0.4f, 1f),
                                $"  - {CurrencyFormat.ExWithDiv(e.TotalValueEx, divinePerExalted)}");
                        }
                        else
                        {
                            ImGui.TextUnformatted($"{e.Name} x{e.Count}");
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(1f, 0.55f, 0.23f, 1f),
                                $"  {CurrencyFormat.ExWithDiv(e.TotalValueEx, divinePerExalted)}");
                        }
                    }
                }
                ImGui.EndChild();
            }

            ImGui.End();
        }
        finally
        {
            Theme.Pop();
        }
    }

    private static string Dur(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }
}
