namespace ThemedWeatherImages;

public class WeatherResponse
{
    public WeatherResponse()
    {
    }

    public WeatherResponse(string? locationName, string? displayTemperature, string? temperatureFahrenheit, string? temperatureCelsius, string? condition, string? conditionCode, string? conditionCategory, string? locationCountry, string? responseSummary)
    {
        LocationName = locationName;
        DisplayTemperature = displayTemperature;
        TemperatureFahrenheit = temperatureFahrenheit;
        TemperatureCelsius = temperatureCelsius;
        Condition = condition;
        ConditionCode = conditionCode;
        ConditionCategory = conditionCategory;
        LocationCountry = locationCountry;
        ResponseSummary = responseSummary;
    }

    public string? Condition { get; set; }
    public string? ConditionCategory { get; set; }
    public string? ConditionCode { get; set; }
    public string? DisplayTemperature { get; set; }
    public string? LocationCountry { get; set; }
    public string? LocationName { get; set; }
    public string? ResponseSummary { get; set; }
    public string? TemperatureCelsius { get; set; }
    public string? TemperatureFahrenheit { get; set; }
}
