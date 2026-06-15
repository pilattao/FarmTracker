using System;
using ImGuiNET;
using FarmTracker.Aggregation;
using FarmTracker.Formatting;
using FarmTracker.Model;
using FarmTracker.Tracking;

namespace FarmTracker.UI;

public sealed class FarmWindow
{
    public void Draw(RunTracker tracker, SessionStatsResult stats, double divinePerExalted,
                     bool bridgeAvailable, bool pricesReady, Settings settings,
                     DateTime nowUtc, Action onStart, Action onStop, Action onReset, ref bool showWindow)
    {
        Theme.Push();
        try
        {
            if (!ImGui.Begin("Farm Tracker", ref showWindow)) { ImGui.End(); return; }

            if (!bridgeAvailable)
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.4f, 0.4f, 1), "NinjaPricer not loaded — income not valued.");
            else if (!pricesReady)
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.85f, 0.3f, 1), "Waiting for price data...");

            var s = tracker.Session;

            // Session summary
            ImGui.TextColored(new System.Numerics.Vector4(0.55f, 0.85f, 1f, 1f),
                $"Profit: {CurrencyFormat.ExWithDiv(s.ProfitEx, divinePerExalted)}");
            ImGui.SameLine();
            ImGui.TextDisabled($"   |   {CurrencyFormat.ExWithDiv(stats.ProfitPerHourEx, divinePerExalted)}/hr");
            ImGui.Text($"Maps: {stats.MapCount}   ·   {stats.MapsPerHour:0.##}/hr   ·   avg {CurrencyFormat.ExWithDiv(stats.AvgProfitPerMapEx, divinePerExalted)}");
            ImGui.Text($"Income {CurrencyFormat.ExWithDiv(s.IncomeEx, divinePerExalted)}   ·   Cost {CurrencyFormat.ExWithDiv(s.CostEx, divinePerExalted)}   ·   Elapsed {FormatDuration(stats.ElapsedSeconds)}");
            if (s.UnpricedPickups > 0)
                ImGui.TextDisabled($"{s.UnpricedPickups} unpriced items picked up");

            if (ImGui.Button("Start")) onStart();
            ImGui.SameLine(); if (ImGui.Button("Stop")) onStop();
            ImGui.SameLine(); if (ImGui.Button("Reset")) onReset();
            ImGui.Separator();

            // Current map (live), with editable cost
            var run = tracker.CurrentRun;
            if (run != null)
            {
                var dur = (nowUtc - run.StartUtc).TotalSeconds;
                ImGui.TextUnformatted($"Current: {run.MapName}");
                ImGui.Text($"  {FormatDuration(dur)}  ·  income {CurrencyFormat.ExWithDiv(run.IncomeEx, divinePerExalted)}  ·  profit {CurrencyFormat.ExWithDiv(run.ProfitEx, divinePerExalted)}");
                float cost = (float)run.CostEx;   // InputFloat needs a float ref; CostEx is double
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputFloat("Map cost (ex)##cur", ref cost)) tracker.SetRunCost(run, cost);
            }
            else
            {
                ImGui.TextDisabled("No active map.");
            }
            ImGui.Separator();

            // History table
            var flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable
                      | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY;
            if (ImGui.BeginTable("ft_runs", 5, flags))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 36);
                ImGui.TableSetupColumn("Map", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Income", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Profit", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableHeadersRow();

                for (var i = s.Runs.Count - 1; i >= 0; i--)   // newest first
                {
                    var r = s.Runs[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(r.Index.ToString());
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(r.MapName);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(FormatDuration(((r.EndUtc ?? nowUtc) - r.StartUtc).TotalSeconds));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(CurrencyFormat.ExWithDiv(r.IncomeEx, divinePerExalted));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(CurrencyFormat.ExWithDiv(r.ProfitEx, divinePerExalted));
                }
                ImGui.EndTable();
            }

            ImGui.End();
        }
        finally
        {
            Theme.Pop();
        }
    }

    private static string FormatDuration(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h{t.Minutes:00}m" : $"{t.Minutes}m{t.Seconds:00}s";
    }
}
