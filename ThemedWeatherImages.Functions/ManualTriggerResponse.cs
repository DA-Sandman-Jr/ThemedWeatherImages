using ThemedWeatherImages.Functions.Generation;

namespace ThemedWeatherImages.Functions;

internal sealed class ManualTriggerResponse
{
    public required int Hour { get; init; }

    public required bool Force { get; init; }

    public required string Subject { get; init; }

    public required GenerationDateResult[] Dates { get; init; }

    public static ManualTriggerResponse FromResults(
        ManualTriggerRequest request,
        string subject,
        IReadOnlyList<GenerationResult> results) =>
        new()
        {
            Hour = request.EffectiveHour,
            Force = request.ForceRegeneration,
            Subject = subject,
            Dates = results.Select(r => new GenerationDateResult
            {
                Date = r.Date,
                FileName = r.FileName,
                Category = r.Category,
                Status = r.Status,
                Message = r.Message
            }).ToArray()
        };

    public sealed class GenerationDateResult
    {
        public required DateTime Date { get; init; }

        public required string FileName { get; init; }

        public required string Category { get; init; }

        public required string Status { get; init; }

        public string? Message { get; init; }
    }
}
