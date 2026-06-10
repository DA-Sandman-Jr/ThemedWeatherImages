using Microsoft.Extensions.Options;

namespace ThemedWeatherImages;

public interface IPromptBuilder
{
    string BuildPrompt(string category);
}

public sealed class PromptBuilder : IPromptBuilder
{
    private readonly INamingUtilities _namingUtilities;
    private readonly IOptions<ThemedWeatherImagesOptions> _options;

    public PromptBuilder(INamingUtilities namingUtilities, IOptions<ThemedWeatherImagesOptions> options)
    {
        _namingUtilities = namingUtilities ?? throw new ArgumentNullException(nameof(namingUtilities));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string BuildPrompt(string category)
    {
        string template = _options.Value.Generation.PromptTemplate
            ?? throw new InvalidOperationException("ThemedWeatherImages requires Generation.PromptTemplate to be configured by the host.");

        string subject = _namingUtilities.GetSubjectName();
        return template
            .Replace("{Subject}", subject, StringComparison.Ordinal)
            .Replace("{Animal}", subject, StringComparison.Ordinal)
            .Replace("{Category}", category, StringComparison.Ordinal);
    }
}
