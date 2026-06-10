using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ThemedWeatherImages.Functions.Generation;

public sealed class ImageGenerationService
{
    private readonly IAiHordeClient _aiHordeClient;
    private readonly IGeneratedImageStore _imageStore;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly INamingUtilities _namingUtilities;
    private readonly IPromptBuilder _promptBuilder;

    public ImageGenerationService(
        INamingUtilities namingUtilities,
        IPromptBuilder promptBuilder,
        IGeneratedImageStore imageStore,
        IAiHordeClient aiHordeClient,
        ILogger<ImageGenerationService> logger)
    {
        _namingUtilities = namingUtilities;
        _promptBuilder = promptBuilder;
        _imageStore = imageStore;
        _aiHordeClient = aiHordeClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GenerationResult>> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        await _imageStore.EnsureReadyAsync(cancellationToken);

        string category = SelectCategoryForHour(request.EffectiveHour);
        _logger.LogInformation("Selected category by UTC hour ({Hour}): {Category}", request.EffectiveHour, category);

        string cleanCategory = NormalizeCategoryForFileName(category);
        string prefix = _namingUtilities.GetFileNamePrefix();
        string subject = _namingUtilities.GetSubjectName();
        string prompt = _promptBuilder.BuildPrompt(category);
        var results = new List<GenerationResult>();

        foreach (DateTime date in request.Dates)
        {
            string dateTag = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            string requestId = $"{prefix}-{cleanCategory}-{dateTag}";
            string fileName = $"{requestId}.webp";

            if (!request.ForceRegeneration && await _imageStore.ImageExistsAsync(fileName, cancellationToken))
            {
                _logger.LogInformation("{FileName} already exists, skipping.", fileName);
                results.Add(GenerationResult.Skipped(fileName, category, date, "Blob already exists."));
                continue;
            }

            try
            {
                AiHordeSubmissionResult submission = await _aiHordeClient.SubmitGenerationAsync(
                    new AiHordeGenerationRequest(prompt, subject, category, dateTag),
                    cancellationToken);

                if (!submission.IsSuccess)
                {
                    results.Add(GenerationResult.Failed(fileName, category, date, submission.FailureMessage ?? "Generation submission failed."));
                    continue;
                }

                string hordeId = submission.HordeId!;
                await _imageStore.SaveRequestMappingAsync(hordeId, fileName, cancellationToken);
                _logger.LogInformation("Mapped Horde ID '{HordeId}' to filename '{FileName}'", hordeId, fileName);
                results.Add(GenerationResult.Submitted(fileName, category, date, hordeId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting for '{Category}' on {DateTag}", category, dateTag);
                results.Add(GenerationResult.Failed(fileName, category, date, ex.Message));
            }
        }

        return results;
    }

    internal static string SelectCategoryForHour(int hour)
    {
        string[] categories = WeatherConditionCategories.ConditionGroups.Keys
            .Where(k => !string.Equals(k, "Unknown", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (categories.Length == 0)
        {
            throw new InvalidOperationException("No weather condition categories are configured.");
        }

        int index = hour % categories.Length;
        if (index < 0)
        {
            index += categories.Length;
        }

        return categories[index];
    }

    private static string NormalizeCategoryForFileName(string category) =>
        category.ToLowerInvariant().Replace(" ", "-").Replace("/", "-");
}
