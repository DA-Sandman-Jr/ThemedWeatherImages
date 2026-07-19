using Microsoft.Extensions.Logging;

namespace ThemedWeatherImages.Functions;

/// <summary>
/// Source-generated <see cref="ILogger"/> extension methods (CA1848). Centralizing
/// them here keeps the call sites free of inline format-string allocations.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Webhook received from AI Horde")]
    public static partial void WebhookReceived(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Rejected webhook payload. Reason: {Reason}. URI: {Img}")]
    public static partial void RejectedWebhookPayload(this ILogger logger, string? reason, string img);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Looking up Horde request ID: {RequestId}")]
    public static partial void LookingUpHordeRequestId(this ILogger logger, string requestId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "No filename mapping found for Horde request ID: {RequestId}. Aborting.")]
    public static partial void NoFilenameMappingFound(this ILogger logger, string requestId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Unable to retrieve AI Horde image from {Uri}. Reason: {Reason}")]
    public static partial void UnableToRetrieveImage(this ILogger logger, Uri uri, string? reason);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Budget kill switch disabled scheduled image generation until {DisabledUntil}.")]
    public static partial void BudgetKillSwitchDisabled(this ILogger logger, DateTimeOffset disabledUntil);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Budget kill switch failed to disable scheduled image generation.")]
    public static partial void BudgetKillSwitchFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Submitting AI Horde generation for '{Category}' on {DateTag} using model {Model}, sampler {SamplerName}, size {Width}x{Height}.")]
    public static partial void SubmittingGeneration(this ILogger logger, string category, string dateTag, string model, string samplerName, int width, int height);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Submitted generation for '{Category}' on {DateTag}: {StatusCode}")]
    public static partial void SubmittedGeneration(this ILogger logger, string category, string dateTag, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Generation submission failed: {Response}")]
    public static partial void GenerationSubmissionFailed(this ILogger logger, string response);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Response did not include 'id' field.")]
    public static partial void GenerationResponseMissingId(this ILogger logger);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Selected category by UTC hour ({Hour}): {Category}")]
    public static partial void SelectedCategoryForHour(this ILogger logger, int hour, string category);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "{FileName} already exists, skipping.")]
    public static partial void FileAlreadyExists(this ILogger logger, string fileName);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Mapped Horde ID '{HordeId}' to filename '{FileName}'")]
    public static partial void MappedHordeIdToFileName(this ILogger logger, string hordeId, string fileName);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error submitting for '{Category}' on {DateTag}")]
    public static partial void ErrorSubmittingGeneration(this ILogger logger, Exception exception, string category, string dateTag);

    [LoggerMessage(EventId = 16, Level = LogLevel.Warning, Message = "Failed to parse manual trigger request body as JSON.")]
    public static partial void FailedToParseManualTriggerBody(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 17, Level = LogLevel.Warning, Message = "Scheduled image generation skipped because the budget kill switch is active.")]
    public static partial void ScheduledGenerationSkipped(this ILogger logger);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Scheduled image generation completed. Submitted={Submitted}, Skipped={Skipped}, Failed={Failed}")]
    public static partial void ScheduledGenerationCompleted(this ILogger logger, int submitted, int skipped, int failed);
}
