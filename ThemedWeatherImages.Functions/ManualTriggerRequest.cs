using System.Collections.Specialized;
using System.Globalization;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Generation;

namespace ThemedWeatherImages.Functions;

internal sealed class ManualTriggerRequest
{
    private readonly List<DateTime> _dates = new();

    private ManualTriggerRequest(string expectedSubjectSlug)
    {
        ExpectedSubjectSlug = expectedSubjectSlug;
        SubjectSlug = expectedSubjectSlug;
        EffectiveHour = DateTime.UtcNow.Hour;
        IsValid = true;
    }

    public bool IsValid { get; private set; }

    public string? ValidationError { get; private set; }

    public int EffectiveHour { get; private set; }

    public bool ForceRegeneration { get; private set; }

    public string ExpectedSubjectSlug { get; }

    public string SubjectSlug { get; private set; }

    public static async Task<ManualTriggerRequest> ParseAsync(
        HttpRequestData request,
        ILogger logger,
        string expectedSubjectSlug,
        string? routeSubject,
        CancellationToken cancellationToken)
    {
        var manualRequest = new ManualTriggerRequest(expectedSubjectSlug);

        if (manualRequest.IsValid && !string.IsNullOrWhiteSpace(routeSubject))
        {
            manualRequest.ApplySubject(routeSubject);
        }

        NameValueCollection query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);

        if (manualRequest.IsValid && query["hour"] is string queryHour)
        {
            manualRequest.ApplyHour(queryHour);
        }

        if (manualRequest.IsValid && query["force"] is string queryForce)
        {
            manualRequest.ApplyForce(queryForce);
        }

        if (manualRequest.IsValid && query["subject"] is string querySubject)
        {
            manualRequest.ApplySubject(querySubject);
        }

        if (manualRequest.IsValid && query["animal"] is string queryAnimal)
        {
            manualRequest.ApplySubject(queryAnimal);
        }

        if (request.Body.CanSeek)
        {
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        string body;
        using (var reader = new StreamReader(request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            manualRequest.ApplyBody(body, logger);
        }

        manualRequest.NormalizeDates();
        return manualRequest;
    }

    public DateTime[] GetDatesToProcess() => _dates.ToArray();

    public ImageGenerationRequest ToGenerationRequest() =>
        new(EffectiveHour, ForceRegeneration, GetDatesToProcess());

    private void ApplyBody(string body, ILogger logger)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                MarkInvalid("Request body must be a JSON object.");
                return;
            }

            if (IsValid && root.TryGetProperty("hour", out JsonElement hourElement))
            {
                ApplyHour(hourElement);
            }

            if (IsValid && root.TryGetProperty("force", out JsonElement forceElement))
            {
                ApplyForce(forceElement);
            }

            if (IsValid && root.TryGetProperty("subject", out JsonElement subjectElement))
            {
                ApplySubject(subjectElement);
            }

            if (IsValid && root.TryGetProperty("animal", out JsonElement animalElement))
            {
                ApplySubject(animalElement);
            }

            if (IsValid && root.TryGetProperty("dates", out JsonElement datesElement))
            {
                ApplyDates(datesElement);
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse manual trigger request body as JSON.");
            MarkInvalid("Malformed JSON payload.");
        }
    }

    private void NormalizeDates()
    {
        if (!IsValid)
        {
            return;
        }

        if (!_dates.Any())
        {
            DateTime utcNow = DateTime.UtcNow;
            _dates.Add(utcNow);
            _dates.Add(utcNow.AddDays(1));
        }

        DateTime[] orderedDates = _dates
            .Select(d => d.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToArray();

        _dates.Clear();
        _dates.AddRange(orderedDates);
    }

    private void ApplyHour(string rawHour)
    {
        if (!int.TryParse(rawHour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour))
        {
            MarkInvalid("Hour must be an integer between 0 and 23.");
            return;
        }

        ApplyHour(hour);
    }

    private void ApplyHour(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetInt32(out int number):
                ApplyHour(number);
                break;
            case JsonValueKind.String:
                ApplyHour(element.GetString() ?? string.Empty);
                break;
            default:
                MarkInvalid("Hour must be provided as a number or numeric string.");
                break;
        }
    }

    private void ApplyHour(int hour)
    {
        if (hour is < 0 or > 23)
        {
            MarkInvalid("Hour must be between 0 and 23.");
            return;
        }

        EffectiveHour = hour;
    }

    private void ApplyForce(string value)
    {
        if (TryParseBoolean(value, out bool parsed))
        {
            ForceRegeneration = parsed;
        }
        else
        {
            MarkInvalid("Force flag must be a boolean value.");
        }
    }

    private void ApplyForce(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                ForceRegeneration = true;
                break;
            case JsonValueKind.False:
                ForceRegeneration = false;
                break;
            case JsonValueKind.String:
                ApplyForce(element.GetString() ?? string.Empty);
                break;
            default:
                MarkInvalid("Force flag must be a boolean value.");
                break;
        }
    }

    private void ApplySubject(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            MarkInvalid("Subject identifier is required when provided.");
            return;
        }

        string normalized = slug.Trim().ToLowerInvariant();
        if (!string.Equals(normalized, ExpectedSubjectSlug, StringComparison.OrdinalIgnoreCase))
        {
            MarkInvalid($"This function is configured for '{ExpectedSubjectSlug}'. Received '{slug}'.");
            return;
        }

        SubjectSlug = normalized;
    }

    private void ApplySubject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                ApplySubject(element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Null:
                break;
            default:
                MarkInvalid("Subject identifier must be provided as a string.");
                break;
        }
    }

    private void ApplyDates(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            MarkInvalid("Dates must be provided as an array.");
            return;
        }

        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && TryParseDate(item.GetString(), out DateTime date))
            {
                _dates.Add(date);
            }
            else if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out long binary))
            {
                var binaryDate = DateTime.FromBinary(binary);
                _dates.Add(binaryDate);
            }
            else
            {
                MarkInvalid("Dates array may only contain ISO-8601 strings or DateTime binary values.");
                return;
            }
        }

        if (!_dates.Any())
        {
            MarkInvalid("At least one valid date is required when specifying the dates array.");
        }
    }

    private static bool TryParseBoolean(string? value, out bool result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = false;
            return false;
        }

        value = value.Trim();

        if (bool.TryParse(value, out result))
        {
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
        {
            result = numeric != 0;
            return true;
        }

        return false;
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = default;
            return false;
        }

        string[] formats = ["yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"];

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
        {
            return true;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);
    }

    private void MarkInvalid(string message)
    {
        IsValid = false;
        ValidationError = message;
    }
}
