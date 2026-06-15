using FarmTracker.Model;
using Xunit;

namespace FarmTracker.Tests;

public class ModelProfitTests
{
    [Fact]
    public void Run_profit_subtracts_cost_and_spent()
    {
        var r = new RunRecord { IncomeEx = 30, CostEx = 5, SpentEx = 2 };
        Assert.Equal(23, r.ProfitEx);
    }

    [Fact]
    public void Session_profit_subtracts_cost_and_spent_and_has_loot_list()
    {
        var s = new Session { IncomeEx = 100, CostEx = 10, SpentEx = 4 };
        Assert.Equal(86, s.ProfitEx);
        Assert.NotNull(s.Loot);
        Assert.Empty(s.Loot);
    }
}
