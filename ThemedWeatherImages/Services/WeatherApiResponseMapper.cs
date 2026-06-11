using System.Globalization;
using Newtonsoft.Json.Linq;

namespace ThemedWeatherImages.Services;

public static class WeatherApiResponseMapper
{
    public static WeatherResponse Map(string content)
    {
        var weatherData = JObject.Parse(content);
        string? locationName = weatherData["location"]?["name"]?.ToString();
        string? locationCountry = weatherData["location"]?["country"]?.ToString();
        string? temperatureFahrenheit = FormatTemperature(weatherData["current"]?["temp_f"]);
        string? temperatureCelsius = FormatTemperature(weatherData["current"]?["temp_c"]);
        bool useFahrenheit = locationCountry is "United States of America" or "USA";
        string displayTemperature = useFahrenheit
            ? (temperatureFahrenheit ?? "N/A") + "\u00b0F"
            : (temperatureCelsius ?? "N/A") + "\u00b0C";

        string? condition = weatherData["current"]?["condition"]?["text"]?.ToString();
        string conditionCode = weatherData["current"]?["condition"]?["code"]?.ToString()
            ?? throw new InvalidOperationException("Weather condition code is missing from API response.");
        string conditionCategory = WeatherConditionCategories.GetConditionCategory(conditionCode);
        string responseSummary = $"The current temperature in {locationName} is {displayTemperature} with {condition} conditions.";

        return new WeatherResponse(
            locationName,
            displayTemperature,
            temperatureFahrenheit,
            temperatureCelsius,
            condition,
            conditionCode,
            conditionCategory,
            locationCountry,
            responseSummary
        );
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
