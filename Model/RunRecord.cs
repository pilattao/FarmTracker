using System;

namespace FarmTracker.Model;

public sealed class RunRecord
{
    public int Index { get; set; }
    public string MapName { get; set; } = "";
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public double IncomeEx { get; set; }
    public double CostEx { get; set; }
    public double ProfitEx => IncomeEx - CostEx;
}
