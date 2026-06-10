using ThemedWeatherImages.Hosting;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class HostRuleParserTests
{
    [Fact]
    public void WildcardHostDoesNotMatchApexDomain()
    {
        Assert.True(HostRuleParser.TryParse("*.aihorde.net", out ParsedHostEntry entry, out string? error), error);

        Assert.False(entry.MatchesHost("aihorde.net", 443));
    }

    [Theory]
    [InlineData("cdn.aihorde.net")]
    [InlineData("images.cdn.aihorde.net")]
    public void WildcardHostMatchesSubdomains(string host)
    {
        Assert.True(HostRuleParser.TryParse("*.aihorde.net", out ParsedHostEntry entry, out string? error), error);

        Assert.True(entry.MatchesHost(host, 443));
    }

    [Fact]
    public void GenerationOptionsUseCurrentAiHordeDefaults()
    {
        var options = new ThemedWeatherImagesOptions();

        Assert.Equal("deliberate", options.Generation.Model);
        Assert.Equal(12d, options.Generation.CfgScale);
        Assert.Equal("k_euler", options.Generation.SamplerName);
        Assert.Equal(512, options.Generation.Width);
        Assert.Equal(512, options.Generation.Height);
    }
}
