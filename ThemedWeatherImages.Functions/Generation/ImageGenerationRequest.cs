namespace ThemedWeatherImages.Functions.Generation;

public sealed record ImageGenerationRequest(int EffectiveHour, bool ForceRegeneration, IReadOnlyList<DateTime> Dates);
