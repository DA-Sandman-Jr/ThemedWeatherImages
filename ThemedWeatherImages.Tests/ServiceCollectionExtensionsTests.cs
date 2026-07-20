using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGenerationSupport_RegistersSystemTimeProviderByDefault()
    {
        var services = new ServiceCollection();

        services.AddThemedWeatherImageGenerationSupport(ConfigureGeneration);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddGenerationSupport_PreservesHostTimeProvider()
    {
        var customTimeProvider = new TestTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(customTimeProvider);

        services.AddThemedWeatherImageGenerationSupport(ConfigureGeneration);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(customTimeProvider, provider.GetRequiredService<TimeProvider>());
    }

    private static void ConfigureGeneration(ThemedWeatherImagesOptions options)
    {
        options.Theme.DisplayName = "Weather Fox";
        options.Theme.SubjectName = "fox";
        options.Theme.SubjectSlug = "fox";
        options.Theme.ImageFileNamePrefix = "fox";
        options.Images.BlobContainerName = "weather-images";
        options.Generation.PromptTemplate = "A {0} fox";
    }

    private sealed class TestTimeProvider : TimeProvider;
}
