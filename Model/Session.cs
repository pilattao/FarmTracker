using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FarmTracker.Model;

public sealed class Session
{
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }                 // archive time; null while active
    public double IncomeEx { get; set; }
    public double CostEx { get; set; }                    // sum of completed run base costs
    public double SpentEx { get; set; }                   // total in-map consumption (accrued live)
    public int UnpricedPickups { get; set; }
    public List<RunRecord> Runs { get; set; } = new();
    public List<LootEntry> Loot { get; set; } = new();    // full session pickup log
    [JsonIgnore] public double ProfitEx => IncomeEx - CostEx - SpentEx;
}
