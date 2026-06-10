using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ThemedWeatherImages.Services;

namespace ThemedWeatherImages;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThemedWeatherImages(this IServiceCollection services, Action<ThemedWeatherImagesOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ThemedWeatherImagesOptions();
        configure(options);
        options.ValidateForReadApi();

        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton<INamingUtilities, NamingUtilities>();

        return services.AddThemedWeatherImagesApiServices();
    }

    public static IServiceCollection AddThemedWeatherImageGenerationSupport(this IServiceCollection services, Action<ThemedWeatherImagesOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ThemedWeatherImagesOptions();
        configure(options);
        options.ValidateForGeneration();

        services.AddSingleton(Options.Create(options));
        services.TryAddSingleton<INamingUtilities, NamingUtilities>();
        services.TryAddSingleton<IPromptBuilder, PromptBuilder>();
        return services;
    }

    private static IServiceCollection AddThemedWeatherImagesApiServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddSingleton<WeatherApiRequestService>();
        services.AddSingleton<WeatherService>();
        return services;
    }
}
