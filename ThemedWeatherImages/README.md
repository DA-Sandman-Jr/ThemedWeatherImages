# ThemedWeatherImages

ThemedWeatherImages provides reusable ASP.NET Core services for pairing current weather data with pre-generated themed daily images.

The library ships no theme defaults: the consuming host supplies the theme, subject, WeatherAPI credentials, fallback IP, and blob image settings. (Before Forever After, for example, uses it to implement Weather Squirrel.)

```bash
dotnet add package ThemedWeatherImages
```

## Service Overview

Hosts that register the library expose these public routes:

- `GET /api/weather-service/current` - Returns a JSON payload describing the detected location, a human-readable summary, and a `conditionCategory` string such as `Rain`, `Clear`, or `Snow`.
- `GET /api/weather-service/current?lat=32.8&lng=-96.8` - Uses explicit coordinates instead of IP-based fallback.
- `GET /api/weather-images/{fileName}` - Proxies pre-rendered artwork from blob storage.

## Read-Side Host Registration

ASP.NET Core hosts register the read-side API services with explicit options:

```csharp
builder.Services.AddThemedWeatherImages(options =>
{
    options.Theme.DisplayName = "Weather Fox";
    options.Theme.SubjectName = "fox";
    options.Theme.SubjectSlug = "fox";
    options.Theme.ImageFileNamePrefix = "fox";

    options.WeatherApi.ApiKey = builder.Configuration["WeatherApi:ApiKey"];
    options.WeatherApi.DefaultIp = builder.Configuration["WeatherApi:DefaultIp"];
    options.Images.BlobBaseUrl = builder.Configuration["WeatherImages:BlobBaseUrl"];
    options.Images.BlobSasToken = builder.Configuration["WeatherImages:BlobSasToken"];
});
```

Required read-side values are validated during registration. Missing values fail startup instead of falling back to library defaults.
`WeatherImages:BlobSasToken` is optional and lets the backend image proxy read from a private Blob container without exposing direct Blob URLs to browsers.

## Generation Support

Azure Functions hosts register shared generation helpers separately:

```csharp
services.AddThemedWeatherImageGenerationSupport(options =>
{
    options.Theme.DisplayName = configuration["ThemedWeatherImages:Theme:DisplayName"];
    options.Theme.SubjectName = configuration["ThemedWeatherImages:Theme:SubjectName"];
    options.Theme.SubjectSlug = configuration["ThemedWeatherImages:Theme:SubjectSlug"];
    options.Theme.ImageFileNamePrefix = configuration["ThemedWeatherImages:Theme:ImageFileNamePrefix"];
    options.Images.BlobContainerName = configuration["ThemedWeatherImages:Images:BlobContainerName"];
    options.Generation.PromptTemplate = configuration["ThemedWeatherImages:Generation:PromptTemplate"];
    options.Generation.Model = configuration["ThemedWeatherImages:Generation:Model"] ?? options.Generation.Model;
    options.Generation.CfgScale = configuration.GetValue<double?>("ThemedWeatherImages:Generation:CfgScale") ?? options.Generation.CfgScale;
    options.Generation.SamplerName = configuration["ThemedWeatherImages:Generation:SamplerName"] ?? options.Generation.SamplerName;
    options.Generation.Width = configuration.GetValue<int?>("ThemedWeatherImages:Generation:Width") ?? options.Generation.Width;
    options.Generation.Height = configuration.GetValue<int?>("ThemedWeatherImages:Generation:Height") ?? options.Generation.Height;
});
```

The website host should not pass Azure Function trigger URLs, function keys, AI Horde credentials, webhook secrets, or generation scheduling values.

## Basic Frontend Consumption

The frontend remains ordinary HTTP consumption: call `/api/weather-service/current`, then probe image candidates such as:

```text
/api/weather-images/fox-rain-20260525.webp
```

The frontend owns presentation and fallback UX. The backend owns weather lookup, condition mapping, image proxying, and cache headers.
