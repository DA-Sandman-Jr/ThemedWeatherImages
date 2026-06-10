using System.Text;

namespace ThemedWeatherImages.Services;

public static class WeatherApiUrlSanitizer
{
    public static string Sanitize(string url)
    {
        const string keyParam = "key=";

        int index = url.IndexOf(keyParam, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return url;
        }

        int valueStart = index + keyParam.Length;
        int valueEnd = url.IndexOf('&', valueStart);
        if (valueEnd < 0)
        {
            valueEnd = url.Length;
        }

        var builder = new StringBuilder(url.Length - (valueEnd - valueStart) + 4);
        builder.Append(url, 0, valueStart);
        builder.Append("****");
        if (valueEnd < url.Length)
        {
            builder.Append(url, valueEnd, url.Length - valueEnd);
        }

        return builder.ToString();
    }
}
