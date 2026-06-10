namespace ThemedWeatherImages.Functions.Generation;

public sealed record GenerationResult(string FileName, string Category, DateTime Date, string Status, string? Message)
{
    public static GenerationResult Submitted(string fileName, string category, DateTime date, string hordeId) =>
        new(fileName, category, date, GenerationStatus.Submitted, $"Submitted with Horde ID {hordeId}.");

    public static GenerationResult Skipped(string fileName, string category, DateTime date, string reason) =>
        new(fileName, category, date, GenerationStatus.Skipped, reason);

    public static GenerationResult Failed(string fileName, string category, DateTime date, string reason) =>
        new(fileName, category, date, GenerationStatus.Failed, reason);
}

public static class GenerationStatus
{
    public const string Submitted = "Submitted";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}
