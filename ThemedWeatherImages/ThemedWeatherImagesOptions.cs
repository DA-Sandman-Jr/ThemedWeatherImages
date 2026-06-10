namespace ThemedWeatherImages;

public sealed class ThemedWeatherImagesOptions
{
    public ThemedWeatherImagesThemeOptions Theme { get; } = new();

    public ThemedWeatherImagesWeatherApiOptions WeatherApi { get; } = new();

    public ThemedWeatherImagesImageOptions Images { get; } = new();

    public ThemedWeatherImageGenerationOptions Generation { get; } = new();

    internal void ValidateForReadApi()
    {
        ValidateTheme();
        Require(WeatherApi.ApiKey, "WeatherApi.ApiKey");
        Require(WeatherApi.DefaultIp, "WeatherApi.DefaultIp");
        Require(Images.BlobBaseUrl, "Images.BlobBaseUrl");
    }

    internal void ValidateForGeneration()
    {
        ValidateTheme();
        Require(Images.BlobContainerName, "Images.BlobContainerName");
        Require(Generation.PromptTemplate, "Generation.PromptTemplate");
        Require(Generation.Model, "Generation.Model");
        Require(Generation.SamplerName, "Generation.SamplerName");
        RequirePositive(Generation.CfgScale, "Generation.CfgScale");
        RequirePositive(Generation.Width, "Generation.Width");
        RequirePositive(Generation.Height, "Generation.Height");
    }

    private void ValidateTheme()
    {
        Require(Theme.DisplayName, "Theme.DisplayName");
        Require(Theme.SubjectName, "Theme.SubjectName");
        Require(Theme.SubjectSlug, "Theme.SubjectSlug");
        Require(Theme.ImageFileNamePrefix, "Theme.ImageFileNamePrefix");
    }

    private static void Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"ThemedWeatherImages requires {name} to be configured by the host.");
        }
    }

    private static void RequirePositive(double value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"ThemedWeatherImages requires {name} to be greater than zero.");
        }
    }
}

public sealed class ThemedWeatherImagesThemeOptions
{
    public string? DisplayName { get; set; }

    public string? SubjectName { get; set; }

    public string? SubjectSlug { get; set; }

    public string? ImageFileNamePrefix { get; set; }
}

public sealed class ThemedWeatherImagesWeatherApiOptions
{
    public string? ApiKey { get; set; }

    public string? DefaultIp { get; set; }

    public bool ExposeErrorDiagnostics { get; set; }
}

public sealed class ThemedWeatherImagesImageOptions
{
    public string? BlobBaseUrl { get; set; }

    public string? BlobSasToken { get; set; }

    public string? BlobContainerName { get; set; }
}

public sealed class ThemedWeatherImageGenerationOptions
{
    public string? PromptTemplate { get; set; }

    public string Model { get; set; } = "deliberate";

    public double CfgScale { get; set; } = 12;

    public string SamplerName { get; set; } = "k_euler";

    public int Width { get; set; } = 512;

    public int Height { get; set; } = 512;
}
