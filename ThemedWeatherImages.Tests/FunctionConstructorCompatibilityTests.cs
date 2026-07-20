using ThemedWeatherImages.Functions;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class FunctionConstructorCompatibilityTests
{
    [Fact]
    public void Constructors_WithoutTimeProvider_RemainAvailable()
    {
        Assert.NotNull(new BudgetKillSwitchFunction(null!));
        Assert.NotNull(new ScheduledImageRequestFunction(null!, null!));
        Assert.NotNull(new SubmitImageRequestFunction(null!, null!));
    }
}
