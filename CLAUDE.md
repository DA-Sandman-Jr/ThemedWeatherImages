# ThemedWeatherImages - Agent Instructions

<!-- B44 ORGANIZATION GUIDANCE: START -->
## B44 Organization Guidance

- `AGENTS.md` files are auto-generated from sibling `CLAUDE.md` by the opt-in `B44.Standards` build target. Edit the `CLAUDE.md`, not the `AGENTS.md`.
- Before editing or reviewing a file, read and follow every applicable `CLAUDE.md` from the repository root through that file's directory. Nearer instructions override broader instructions.
- Analyzer severities live in the `B44.Standards` packaged globalconfig, never in a repository `.editorconfig`. Repository editorconfigs own style and whitespace only; tune analyzer policy upstream in the package.
- Fix shared behavior in the B44 package that owns it; do not fork or paste a local copy into a consumer repository. Keep runtime `B44.Common` references exact. Active, unreleased repositories may use a bounded patch float for `B44.Standards` (for example, `0.4.*`); released or production repositories pin it exactly. Enforcement-expanding Standards changes bump the minor version and never enter an existing patch float.
- Treat roughly 350 physical lines as a review warning for production source files. New production files should normally stay at or below 500 lines; files above 650 lines require a clear cohesion-based reason.
- Existing oversized files must not grow unless the same change performs a real extraction and leaves the file smaller. Coordinators coordinate; do not evade the limit with cosmetic partial classes, one-method services, generic utility dumping grounds, or needless factories.
- Before automated analyzer fixes, baseline measurement, scripted bulk text rewrites, or consuming a freshly published package, read `.b44/B44.Tooling.md`.
<!-- B44 ORGANIZATION GUIDANCE: END -->

ThemedWeatherImages provides reusable .NET 8 building blocks for themed daily weather imagery: an ASP.NET Core read-side API (WeatherAPI lookup, condition categorization, blob-backed image proxying) and Azure Functions isolated-worker classes for background image generation via AI Horde. Both ship as NuGet packages; consumers supply every theme-specific value.

## Commands

Restore dependencies:

```bash
dotnet restore ThemedWeatherImages.sln
```

Build the solution:

```bash
dotnet build ThemedWeatherImages.sln
```

Run tests:

```bash
dotnet test ThemedWeatherImages.sln
```

Pack the NuGet payloads:

```bash
dotnet pack ThemedWeatherImages.sln -c Release
```

Run the reference web host for local testing:

```bash
dotnet run --project ThemedWeatherImages.Host
```

## Architecture

- `ThemedWeatherImages/` is the packable read-side library: weather controllers, WeatherAPI services, condition categories, naming utilities, options, and `AddThemedWeatherImages()` registration.
- `ThemedWeatherImages.Functions/` is the packable Azure Functions class library: `[Function]` classes (scheduled/manual submission, AI Horde webhook, budget kill switch), generation services, and blob/table stores. It contains no `Program.cs` or `host.json`; hosts own those.
- `ThemedWeatherImages.Tests/` covers both libraries with xUnit.
- `ThemedWeatherImages.Host/` is a runnable reference web host for local testing only; it is not packed or shipped.
- `ThemedWeatherImages.FunctionsHost/` is a runnable reference Functions host for local testing only; it also proves that the worker discovers functions from the referenced library.

## Key Conventions

- No theme defaults in the libraries. Hosts pass theme, credentials, and storage settings through `AddThemedWeatherImages(...)` / `AddThemedWeatherImageGenerationSupport(...)`; required values fail fast at registration.
- Library code and settings use theme-neutral names only: the scheduled trigger reads `%THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE%`, and default table names are `themedWeatherImagesControls` and `hordeRequestMappings` (constructor-overridable).
- The consumer-facing NuGet surface is `ThemedWeatherImages/` and `ThemedWeatherImages.Functions/`. Do not move product logic into the reference hosts.
- Preserve the documented route contracts `GET /api/weather-service/current` and `GET /api/weather-images/{fileName}` and the four function names unless a breaking change is intentional and documented — Before Forever After consumes both packages in production.
- Do not commit secrets. `local.settings.json` is gitignored; `local.settings.sample.json` carries placeholders only.
- Update `README.md` and the per-package README files when changing registration surface, routes, function names, or configuration keys.
- Run `scripts/install-hooks.sh` once per clone; the pre-commit hook runs `dotnet format --verify-no-changes` on staged C# files.

## Configuration Notes

Read-side hosts bind: theme values (host-chosen section), `WeatherApi:ApiKey`, `WeatherApi:DefaultIp`, `WeatherApi:ExposeErrorDiagnostics`, `WeatherImages:BlobBaseUrl`, `WeatherImages:BlobSasToken` (optional).

Functions hosts bind: `AzureWebJobsStorage`, `AI_HORDE_API_KEY`, `AI_HORDE_API_URL`, `WEBHOOK_URL`, optional `AIHORDE_ALLOWED_IMAGE_HOSTS`, `THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE`, theme/generation options via `AddThemedWeatherImageGenerationSupport`, and the standard `AzureWebJobs.SubmitImageRequestScheduled.Disabled` switch.

## Releases

Pushing a `vX.Y.Z` tag on `main` runs `.github/workflows/release.yml`, which packs both packages at that version and pushes them to nuget.org via Trusted Publishing: `NuGet/login@v1` exchanges the job's OIDC token (`permissions: id-token: write`) for a one-hour API key against the nuget.org policy registered for this repository's `release.yml`. The `NUGET_USER` secret holds the nuget.org profile name; there is no long-lived API key to rotate. `ci.yml` builds, tests, and packs on every push and pull request.
