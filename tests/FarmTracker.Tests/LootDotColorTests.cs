using FarmTracker.UI;
using Xunit;

namespace FarmTracker.Tests;

public class LootDotColorTests
{
    [Fact]
    public void Known_categories_get_distinct_opaque_colors()
    {
        var currency = LootDotColor.For("currency");
        var unique = LootDotColor.For("unique");
        Assert.Equal(1f, currency.W);                       // opaque
        Assert.NotEqual(currency, unique);
    }

    [Fact]
    public void Unknown_category_falls_back_to_neutral_and_is_case_insensitive()
    {
        Assert.Equal(LootDotColor.For("currency"), LootDotColor.For("CURRENCY"));
        var unknown = LootDotColor.For("zzz");
        Assert.Equal(1f, unknown.W);
    }
}
