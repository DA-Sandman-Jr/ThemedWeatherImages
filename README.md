# ThemedWeatherImages

Reusable .NET 8 building blocks for "themed daily weather imagery": look up the visitor's current weather, map it to a condition category, and serve a matching pre-generated image of your theme's subject — plus Azure Functions classes that generate those images in the background via [AI Horde](https://stablehorde.net/).

The libraries are deliberately theme-free plumbing. The consuming host supplies every specific — theme name, subject, prompt template, credentials, storage locations. ([Before Forever After](https://beforeforeverafter.com) supplies a squirrel; yours could supply anything.)

## Packages

| Package | Contents |
|---|---|
| `ThemedWeatherImages` | ASP.NET Core read-side API: `GET /api/weather-service/current` (weather + condition category) and `GET /api/weather-images/{fileName}` (blob-backed image proxy), with `AddThemedWeatherImages()` registration. |
| `ThemedWeatherImages.Functions` | Azure Functions isolated-worker classes: scheduled + manual AI Horde submission, webhook ingestion of finished images, budget kill switch. Reference from your own Functions host. |

```bash
dotnet add package ThemedWeatherImages
dotnet add package ThemedWeatherImages.Functions
```

## Web host quick start

```csharp
builder.Services.AddControllers();
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

Required values are validated at registration; missing ones fail startup with a message naming the option. See [ThemedWeatherImages/README.md](ThemedWeatherImages/README.md) for the full read-side reference.

## Functions host quick start

The Functions package ships function classes only — you own the host. Create an isolated-worker project referencing the package, call `AddThemedWeatherImageGenerationSupport`, register the remaining generation services, and bind your theme (see [ThemedWeatherImages.Functions/README.md](ThemedWeatherImages.Functions/README.md) for the complete registrations and required app settings). That shared registration supplies `TimeProvider.System` unless the host registered a custom `TimeProvider` first. The scheduled trigger reads its NCRONTAB schedule from the `THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE` app setting.

Working examples of both hosts live in this repository:

- [ThemedWeatherImages.Host](ThemedWeatherImages.Host/Program.cs) — minimal web host (set `WeatherApi:ApiKey` via user secrets to exercise it).
- [ThemedWeatherImages.FunctionsHost](ThemedWeatherImages.FunctionsHost/Program.cs) — minimal Functions host (copy `local.settings.sample.json` to `local.settings.json` to run locally).

## Development

```bash
dotnet build ThemedWeatherImages.sln
dotnet test ThemedWeatherImages.sln
```

Run `scripts/install-hooks.sh` once per clone to enable the pre-commit format check. Style violations are build errors (`EnforceCodeStyleInBuild`); fix with `dotnet format`.

A handful of connectivity tests hit live services and only run when their environment variables are configured (`WEATHER_API_KEY`, `WEATHER_IMAGES__GENERATION__MANUALTRIGGERURL`); they skip silently otherwise, including in CI.

## Releasing

CI (`.github/workflows/ci.yml`) builds, tests, and packs on every push and PR. Pushing a tag like `v1.2.0` runs `release.yml`, which packs both packages at version `1.2.0` and pushes them to nuget.org via [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing): a nuget.org policy for this repository's `release.yml` exchanges the workflow's OIDC token for a one-hour API key, so no long-lived publishing secret exists. The `NUGET_USER` repository secret holds the nuget.org profile name the policy belongs to.

## History

Extracted from the [Before Forever After](https://github.com/DA-Sandman-Jr/before-forever-after) repository at commit `3dd3868`; that repo retains the pre-extraction history and now consumes these packages.

## License

[MIT](LICENSE)
