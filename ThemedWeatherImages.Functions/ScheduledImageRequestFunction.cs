using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Generation;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions;

public sealed class ScheduledImageRequestFunction
{
    private readonly IScheduledImageRequestControlStore _controlStore;
    private readonly ImageGenerationService _generationService;
    private readonly TimeProvider _timeProvider;

    public ScheduledImageRequestFunction(
        ImageGenerationService generationService,
        IScheduledImageRequestControlStore controlStore)
        : this(generationService, controlStore, TimeProvider.System)
    {
    }

    public ScheduledImageRequestFunction(
        ImageGenerationService generationService,
        IScheduledImageRequestControlStore controlStore,
        TimeProvider timeProvider)
    {
        _generationService = generationService;
        _controlStore = controlStore;
        _timeProvider = timeProvider;
    }

    [Function("SubmitImageRequestScheduled")]
    public async Task Run(
        [TimerTrigger("%THEMED_WEATHER_IMAGES_GENERATION_SCHEDULE%")] TimerInfo timer,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("SubmitImageRequestScheduled");

        DateTimeOffset requestedAt = _timeProvider.GetUtcNow();

        if (await _controlStore.IsDisabledAsync(requestedAt, context.CancellationToken))
        {
            logger.ScheduledGenerationSkipped();
            return;
        }

        DateTime utcNow = requestedAt.UtcDateTime;

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

        logger.ScheduledGenerationCompleted(
            results.Count(r => string.Equals(r.Status, GenerationStatus.Submitted, StringComparison.Ordinal)),
            results.Count(r => string.Equals(r.Status, GenerationStatus.Skipped, StringComparison.Ordinal)),
            results.Count(r => string.Equals(r.Status, GenerationStatus.Failed, StringComparison.Ordinal)));
    }
}
