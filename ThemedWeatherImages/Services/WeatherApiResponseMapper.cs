using System.Globalization;
using Newtonsoft.Json.Linq;

namespace ThemedWeatherImages.Services;

public static class WeatherApiResponseMapper
{
    public static WeatherResponse Map(string content)
    {
        var weatherData = JObject.Parse(content);
        JToken? location = weatherData["location"];
        JToken? current = weatherData["current"];
        JToken? condition = current?["condition"];

        string? locationName = location?["name"]?.ToString();
        string? locationCountry = location?["country"]?.ToString();
        string? temperatureFahrenheit = FormatTemperature(current?["temp_f"]);
        string? temperatureCelsius = FormatTemperature(current?["temp_c"]);
        string displayTemperature = BuildDisplayTemperature(locationCountry, temperatureFahrenheit, temperatureCelsius);

        string? conditionText = condition?["text"]?.ToString();
        string conditionCode = condition?["code"]?.ToString()
            ?? throw new InvalidOperationException("Weather condition code is missing from API response.");
        string conditionCategory = WeatherConditionCategories.GetConditionCategory(conditionCode);
        string responseSummary = $"The current temperature in {locationName} is {displayTemperature} with {conditionText} conditions.";

        return new WeatherResponse(
            locationName,
            displayTemperature,
            temperatureFahrenheit,
            temperatureCelsius,
            conditionText,
            conditionCode,
            conditionCategory,
            locationCountry,
            responseSummary
        );
    }

    private static string BuildDisplayTemperature(string? locationCountry, string? temperatureFahrenheit, string? temperatureCelsius)
    {
        bool useFahrenheit = locationCountry is "United States of America" or "USA";
        return useFahrenheit
            ? (temperatureFahrenheit ?? "N/A") + "\u00b0F"
            : (temperatureCelsius ?? "N/A") + "\u00b0C";
    }

    // Temperatures are part of the response contract and get parsed by
    // consumers, so the host machine's regional decimal separator must not
    // leak into them.
    private static string? FormatTemperature(JToken? temperature)
    {
        if (temperature is null || temperature.Type == JTokenType.Null)
        {
            return null;
        }

        return temperature.Type is JTokenType.Float or JTokenType.Integer
            ? temperature.Value<double>().ToString(CultureInfo.InvariantCulture)
            : temperature.ToString();
    }
}
