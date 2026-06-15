using System;
using System.Collections.Generic;

namespace FarmTracker.Model;

public sealed class Session
{
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }              // set on Stop; null while running
    public double IncomeEx { get; set; }              // all loot collected this session
    public double CostEx { get; set; }                // sum of completed run costs
    public int UnpricedPickups { get; set; }
    public List<RunRecord> Runs { get; set; } = new(); // completed runs
    public double ProfitEx => IncomeEx - CostEx;
}
