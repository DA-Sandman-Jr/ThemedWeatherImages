# ThemedWeatherImages.Functions

Azure Functions (isolated worker) function classes and generation services for ThemedWeatherImages background image generation: scheduled and manual AI Horde submissions, webhook ingestion of finished images, and a budget kill switch.

This package contains **function classes only** — there is no `Program.cs` or `host.json` here. Reference it from your own Functions host project; the worker SDK discovers the `[Function]` classes from the referenced assembly.

## Functions

- `SubmitImageRequestScheduled` — timer-triggered; schedule comes from the `%THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE%` app setting. Skips work while the budget kill switch is active.
- `SubmitImageRequestManual` — HTTP-triggered manual generation request.
- `AihordeWebhookReceiver` — HTTP-triggered webhook that stores finished AI Horde images in blob storage.
- `DisableScheduledImageRequestBudgetKillSwitch` — HTTP-triggered endpoint for Azure Cost Management budget alerts; disables scheduled generation for a cooldown window.

## Hosting

Create a Functions host project that references this package plus `Microsoft.Azure.Functions.Worker` and `Microsoft.Azure.Functions.Worker.Sdk`, then register the services and supply your theme:

```csharp
services.AddSingleton(new BlobServiceClient(blobConnString));
services.AddSingleton(new HordeRequestMappingStore(blobConnString));
services.AddSingleton<IScheduledImageRequestControlStore>(new ScheduledImageRequestControlStore(blobConnString));
services.AddSingleton<IGeneratedImageStore, BlobGeneratedImageStore>();
services.AddSingleton<IAiHordeClient, AiHordeClient>();
services.AddSingleton<ImageGenerationService>();
services.AddHttpClient();

services.AddThemedWeatherImageGenerationSupport(options =>
{
    options.Theme.DisplayName = config["ThemedWeatherImages:Theme:DisplayName"];
    options.Theme.SubjectName = config["ThemedWeatherImages:Theme:SubjectName"];
    options.Theme.SubjectSlug = config["ThemedWeatherImages:Theme:SubjectSlug"];
    options.Theme.ImageFileNamePrefix = config["ThemedWeatherImages:Theme:ImageFileNamePrefix"];
    options.Images.BlobContainerName = config["ThemedWeatherImages:Images:BlobContainerName"];
    options.Generation.PromptTemplate = config["ThemedWeatherImages:Generation:PromptTemplate"];
});
```

`AddThemedWeatherImageGenerationSupport` registers `TimeProvider.System` only when the host has not already registered a `TimeProvider`. Register a custom provider before this call when the host needs controlled time. The function classes retain their pre-`TimeProvider` constructor overloads for compatibility, but hosts should use this registration so DI selects the injectable-time constructors.

The configuration section names above are the host's choice — bind from whatever section your app owns. See `ThemedWeatherImages.FunctionsHost` in the repository for a complete reference host.

## Required app settings

- `AzureWebJobsStorage` — storage account used for blobs and control tables.
- `AI_HORDE_API_KEY`, `AI_HORDE_API_URL` — AI Horde access.
- `WEBHOOK_URL` — public URL of the webhook receiver.
- `AIHORDE_ALLOWED_IMAGE_HOSTS` — optional comma-separated allow list for image download hosts.
- `THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE` — NCRONTAB expression for the scheduled trigger.
- `AzureWebJobs.SubmitImageRequestScheduled.Disabled` — standard switch to pause the timer function.

## Storage defaults

Table names default to `themedWeatherImagesControls` (kill switch) and `hordeRequestMappings` (request mapping); both are constructor parameters if your deployment already uses different names. The blob container name always comes from `Images.BlobContainerName` in options.
