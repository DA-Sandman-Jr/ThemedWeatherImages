using Xunit;

namespace ThemedWeatherImages.Tests;

public class WeatherConditionCategoriesTests
{
    [Theory]
    [InlineData(WeatherConditionCode.Sunny, "Clear/Sunny")]
    [InlineData(WeatherConditionCode.Cloudy, "Cloudy")]
    [InlineData(WeatherConditionCode.Mist, "Fog/Mist")]
    [InlineData(WeatherConditionCode.PatchyRainPossible, "Rain")]
    [InlineData(WeatherConditionCode.LightSnow, "Snow")]
    [InlineData(WeatherConditionCode.LightSleet, "Ice")]
    [InlineData(WeatherConditionCode.ThunderyOutbreaksPossible, "Thunderstorm")]
    public void GetConditionCategory_ReturnsExpectedCategory(WeatherConditionCode code, string expected)
    {
        string category = WeatherConditionCategories.GetConditionCategory(code);
        Assert.Equal(expected, category);
    }

    [Fact]
    public void GetConditionCategory_Unknown_ReturnsUnknown()
    {
        string category = WeatherConditionCategories.GetConditionCategory((WeatherConditionCode)9999);
        Assert.Equal("Unknown", category);
    }
}
