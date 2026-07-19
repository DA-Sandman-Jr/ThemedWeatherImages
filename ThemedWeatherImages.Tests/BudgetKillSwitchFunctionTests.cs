using System;
using System.Globalization;
using ThemedWeatherImages.Functions;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class BudgetKillSwitchFunctionTests
{
    [Theory]
    [InlineData("2026-05-31T18:00:00Z", "2026-06-01T00:00:00Z")]
    [InlineData("2026-12-31T23:59:59Z", "2027-01-01T00:00:00Z")]
    public void GetStartOfNextUtcMonth_ReturnsNextMonthBoundary(string nowValue, string expectedValue)
    {
        var now = DateTimeOffset.Parse(nowValue, CultureInfo.InvariantCulture);
        var expected = DateTimeOffset.Parse(expectedValue, CultureInfo.InvariantCulture);

        Assert.Equal(expected, BudgetKillSwitchFunction.GetStartOfNextUtcMonth(now));
    }
}
