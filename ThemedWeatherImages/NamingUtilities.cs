using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ThemedWeatherImages;

public interface INamingUtilities
{
    string GetSubjectName();

    string GetSubjectSlug();

    string GetFileNamePrefix();

    string GetContainerName();
}

public sealed class NamingUtilities : INamingUtilities
{
    private static readonly Regex InvalidCharacters = new("[^a-z0-9-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CollapseHyphens = new("-{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IOptions<ThemedWeatherImagesOptions> _options;

    public NamingUtilities(IOptions<ThemedWeatherImagesOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    public string GetSubjectName() => GetSubjectNameFromOptions(_options.Value);

    public string GetSubjectSlug() => GetSubjectSlugFromOptions(_options.Value);

    public string GetFileNamePrefix() => GetFileNamePrefixFromOptions(_options.Value);

    public string GetContainerName() => GetContainerNameFromOptions(_options.Value);

    public static string GetSubjectNameFromOptions(ThemedWeatherImagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Require(options.Theme.SubjectName, "Theme.SubjectName").Trim();
    }

    public static string GetSubjectSlugFromOptions(ThemedWeatherImagesOptions options)
    {
        string slug = Require(options.Theme.SubjectSlug, "Theme.SubjectSlug").Trim().ToLowerInvariant();
        slug = slug.Replace(' ', '-').Replace('/', '-');
        slug = InvalidCharacters.Replace(slug, "-");
        slug = CollapseHyphens.Replace(slug, "-").Trim('-');
        return Require(slug, "Theme.SubjectSlug");
    }

    public static string GetFileNamePrefixFromOptions(ThemedWeatherImagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Require(options.Theme.ImageFileNamePrefix, "Theme.ImageFileNamePrefix").Trim();
    }

    public static string GetContainerNameFromOptions(ThemedWeatherImagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Require(options.Images.BlobContainerName, "Images.BlobContainerName").Trim();
    }

    private static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"ThemedWeatherImages requires {name} to be configured by the host.");
        }

        return value;
    }
}
