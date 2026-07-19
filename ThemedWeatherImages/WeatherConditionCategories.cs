namespace ThemedWeatherImages;

public static class WeatherConditionCategories
{
    public static readonly Dictionary<string, List<WeatherConditionCode>> ConditionGroups = new(StringComparer.Ordinal)
    {
        {
            "Clear/Sunny", new List<WeatherConditionCode>
            {
                WeatherConditionCode.Sunny,
                WeatherConditionCode.Clear
            }
        },
        {
            "Cloudy", new List<WeatherConditionCode>
            {
                WeatherConditionCode.PartlyCloudy,
                WeatherConditionCode.Cloudy,
                WeatherConditionCode.Overcast
            }
        },
        {
            "Fog/Mist", new List<WeatherConditionCode>
            {
                WeatherConditionCode.Mist,
                WeatherConditionCode.Fog,
                WeatherConditionCode.FreezingFog
            }
        },
        {
            "Rain", new List<WeatherConditionCode>
            {
                WeatherConditionCode.PatchyRainPossible,
                WeatherConditionCode.LightRain,
                WeatherConditionCode.ModerateRain,
                WeatherConditionCode.HeavyRain,
                WeatherConditionCode.LightRainShower,
                WeatherConditionCode.ModerateOrHeavyRainShower,
                WeatherConditionCode.TorrentialRainShower,
                WeatherConditionCode.PatchyLightRain,
                WeatherConditionCode.ModerateRainAtTimes,
                WeatherConditionCode.HeavyRainAtTimes,
                WeatherConditionCode.LightFreezingRain,
                WeatherConditionCode.ModerateOrHeavyFreezingRain
            }
        },
        {
            "Snow", new List<WeatherConditionCode>
            {
                WeatherConditionCode.PatchySnowPossible,
                WeatherConditionCode.LightSnow,
                WeatherConditionCode.ModerateSnow,
                WeatherConditionCode.HeavySnow,
                WeatherConditionCode.BlowingSnow,
                WeatherConditionCode.Blizzard,
                WeatherConditionCode.LightSnowShowers,
                WeatherConditionCode.ModerateOrHeavySnowShowers,
                WeatherConditionCode.PatchyLightSnow,
                WeatherConditionCode.PatchyModerateSnow,
                WeatherConditionCode.PatchyHeavySnow,
                WeatherConditionCode.PatchyLightSnowWithThunder,
                WeatherConditionCode.ModerateOrHeavySnowWithThunder
            }
        },
        {
            "Ice", new List<WeatherConditionCode>
            {
                WeatherConditionCode.PatchySleetPossible,
                WeatherConditionCode.LightSleet,
                WeatherConditionCode.ModerateOrHeavySleet,
                WeatherConditionCode.LightSleetShowers,
                WeatherConditionCode.ModerateOrHeavySleetShowers,
                WeatherConditionCode.PatchyFreezingDrizzlePossible,
                WeatherConditionCode.FreezingDrizzle,
                WeatherConditionCode.HeavyFreezingDrizzle,
                WeatherConditionCode.IcePellets,
                WeatherConditionCode.LightShowersOfIcePellets,
                WeatherConditionCode.ModerateOrHeavyShowersOfIcePellets
            }
        },
        {
            "Thunderstorm", new List<WeatherConditionCode>
            {
                WeatherConditionCode.ThunderyOutbreaksPossible,
                WeatherConditionCode.PatchyLightRainWithThunder,
                WeatherConditionCode.ModerateOrHeavyRainWithThunder
            }
        }
    };

    public static string GetConditionCategory(WeatherConditionCode code)
    {
        foreach (KeyValuePair<string, List<WeatherConditionCode>> group in ConditionGroups)
        {
            if (group.Value.Contains(code))
            {
                return group.Key;
            }
        }

        return "Unknown";
    }

    public static string GetConditionCategory(string code)
    {
        if (Enum.TryParse<WeatherConditionCode>(code, out WeatherConditionCode parsedCode))
        {
            return GetConditionCategory(parsedCode);
        }

        return "Unknown";
    }
}
