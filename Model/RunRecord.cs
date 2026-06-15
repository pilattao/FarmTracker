using System;
using System.Text.Json.Serialization;

namespace FarmTracker.Model;

public sealed class RunRecord
{
    public int Index { get; set; }
    public string MapName { get; set; } = "";
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public double IncomeEx { get; set; }
    public double CostEx { get; set; }          // user-entered base cost
    public double SpentEx { get; set; }         // in-map currency consumption (auto-detected)
    [JsonIgnore] public double ProfitEx => IncomeEx - CostEx - SpentEx;
}
