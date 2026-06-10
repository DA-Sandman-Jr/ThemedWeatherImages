using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Generation;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions;

public sealed class ScheduledImageRequestFunction
{
    private readonly IScheduledImageRequestControlStore _controlStore;
    private readonly ImageGenerationService _generationService;

    public ScheduledImageRequestFunction(
        ImageGenerationService generationService,
        IScheduledImageRequestControlStore controlStore)
    {
        _generationService = generationService;
        _controlStore = controlStore;
    }

    [Function("SubmitImageRequestScheduled")]
    public async Task Run(
        [TimerTrigger("%THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE%")] TimerInfo timer,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("SubmitImageRequestScheduled");

        if (await _controlStore.IsDisabledAsync(DateTimeOffset.UtcNow, context.CancellationToken))
        {
            logger.LogWarning("Scheduled image generation skipped because the budget kill switch is active.");
            return;
        }

        DateTime utcNow = DateTime.UtcNow;

        var request = new ImageGenerationRequest(
            utcNow.Hour,
            ForceRegeneration: false,
            [
                utcNow.Date,
                utcNow.Date.AddDays(1)
            ]);

        IReadOnlyList<GenerationResult> results = await _generationService.GenerateAsync(
            request,
            context.CancellationToken);

        logger.LogInformation(
            "Scheduled image generation completed. Submitted={Submitted}, Skipped={Skipped}, Failed={Failed}",
            results.Count(r => string.Equals(r.Status, GenerationStatus.Submitted, StringComparison.Ordinal)),
            results.Count(r => string.Equals(r.Status, GenerationStatus.Skipped, StringComparison.Ordinal)),
            results.Count(r => string.Equals(r.Status, GenerationStatus.Failed, StringComparison.Ordinal)));
    }
}
