namespace ThemedWeatherImages;

public enum WeatherConditionCode
{
    // Clear/Sunny Conditions
    Sunny = 1000,

    Clear = 1000, // Clear is the night-time equivalent of Sunny

    // Cloudy Conditions
    PartlyCloudy = 1003,

    Cloudy = 1006,
    Overcast = 1009,

    // Fog/Mist Conditions
    Mist = 1030,

    Fog = 1135,
    FreezingFog = 1147,

    // Rain Conditions
    PatchyRainPossible = 1063,

    LightRain = 1183,
    ModerateRain = 1189,
    HeavyRain = 1195,
    LightRainShower = 1240,
    ModerateOrHeavyRainShower = 1243,
    TorrentialRainShower = 1246,
    PatchyLightRain = 1180,
    ModerateRainAtTimes = 1186,
    HeavyRainAtTimes = 1192,
    LightFreezingRain = 1198,
    ModerateOrHeavyFreezingRain = 1201,

    // Snow Conditions
    PatchySnowPossible = 1066,

    LightSnow = 1213,
    ModerateSnow = 1219,
    HeavySnow = 1225,
    BlowingSnow = 1114,
    Blizzard = 1117,
    LightSnowShowers = 1255,
    ModerateOrHeavySnowShowers = 1258,
    PatchyLightSnow = 1210,
    PatchyModerateSnow = 1216,
    PatchyHeavySnow = 1222,
    PatchyLightSnowWithThunder = 1279,
    ModerateOrHeavySnowWithThunder = 1282,

    // Sleet Conditions
    PatchySleetPossible = 1069,

    LightSleet = 1204,
    ModerateOrHeavySleet = 1207,
    LightSleetShowers = 1249,
    ModerateOrHeavySleetShowers = 1252,

    // Ice Conditions
    PatchyFreezingDrizzlePossible = 1072,

    FreezingDrizzle = 1168,
    HeavyFreezingDrizzle = 1171,
    IcePellets = 1237,
    LightShowersOfIcePellets = 1261,
    ModerateOrHeavyShowersOfIcePellets = 1264,

    // Thunderstorm Conditions
    ThunderyOutbreaksPossible = 1087,

    PatchyLightRainWithThunder = 1273,
    ModerateOrHeavyRainWithThunder = 1276
}
