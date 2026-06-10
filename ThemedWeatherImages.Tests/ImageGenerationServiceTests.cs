using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ThemedWeatherImages.Functions.Generation;
using Xunit;

namespace ThemedWeatherImages.Tests;

public class ImageGenerationServiceTests
{
    [Theory]
    [InlineData(0, "Clear/Sunny")]
    [InlineData(1, "Cloudy")]
    [InlineData(7, "Clear/Sunny")]
    public void SelectCategoryForHour_UsesHourModuloConfiguredCategories(int hour, string expectedCategory)
    {
        Assert.Equal(expectedCategory, ImageGenerationService.SelectCategoryForHour(hour));
    }

    [Fact]
    public async Task GenerateAsync_SkipsExistingBlobWhenForceIsFalse()
    {
        var store = new FakeGeneratedImageStore("squirrel-clear-sunny-20260531.webp");
        var hordeClient = new RecordingAiHordeClient();
        ImageGenerationService service = CreateService(store, hordeClient);

        IReadOnlyList<GenerationResult> results = await service.GenerateAsync(
            new ImageGenerationRequest(0, ForceRegeneration: false, [new DateTime(2026, 5, 31)]),
            CancellationToken.None);

        GenerationResult result = Assert.Single(results);
        Assert.Equal(GenerationStatus.Skipped, result.Status);
        Assert.Equal("squirrel-clear-sunny-20260531.webp", result.FileName);
        Assert.True(store.EnsureReadyCalled);
        Assert.Empty(hordeClient.Requests);
        Assert.Empty(store.SavedMappings);
    }

    [Fact]
    public async Task GenerateAsync_SubmitsExistingBlobWhenForceIsTrue()
    {
        var store = new FakeGeneratedImageStore("squirrel-clear-sunny-20260531.webp");
        var hordeClient = new RecordingAiHordeClient();
        ImageGenerationService service = CreateService(store, hordeClient);

        IReadOnlyList<GenerationResult> results = await service.GenerateAsync(
            new ImageGenerationRequest(0, ForceRegeneration: true, [new DateTime(2026, 5, 31)]),
            CancellationToken.None);

        GenerationResult result = Assert.Single(results);
        Assert.Equal(GenerationStatus.Submitted, result.Status);
        Assert.Equal("squirrel-clear-sunny-20260531.webp", result.FileName);
        AiHordeGenerationRequest request = Assert.Single(hordeClient.Requests);
        Assert.Equal("Prompt for Clear/Sunny", request.Prompt);
        Assert.Equal("squirrel", request.Subject);
        Assert.Equal("Clear/Sunny", request.Category);
        Assert.Equal("20260531", request.DateTag);
        Assert.Equal(("horde-123", "squirrel-clear-sunny-20260531.webp"), Assert.Single(store.SavedMappings));
    }

    private static ImageGenerationService CreateService(
        FakeGeneratedImageStore store,
        RecordingAiHordeClient hordeClient) =>
        new(
            new FakeNamingUtilities(),
            new FakePromptBuilder(),
            store,
            hordeClient,
            NullLogger<ImageGenerationService>.Instance);

    private sealed class FakeGeneratedImageStore : IGeneratedImageStore
    {
        private readonly HashSet<string> _existingFiles;

        public FakeGeneratedImageStore(params string[] existingFiles)
        {
            _existingFiles = existingFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool EnsureReadyCalled { get; private set; }

        public List<(string HordeId, string FileName)> SavedMappings { get; } = new();

        public Task EnsureReadyAsync(CancellationToken cancellationToken)
        {
            EnsureReadyCalled = true;
            return Task.CompletedTask;
        }

        public Task<bool> ImageExistsAsync(string fileName, CancellationToken cancellationToken) =>
            Task.FromResult(_existingFiles.Contains(fileName));

        public Task SaveRequestMappingAsync(string hordeId, string fileName, CancellationToken cancellationToken)
        {
            SavedMappings.Add((hordeId, fileName));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAiHordeClient : IAiHordeClient
    {
        public List<AiHordeGenerationRequest> Requests { get; } = new();

        public Task<AiHordeSubmissionResult> SubmitGenerationAsync(
            AiHordeGenerationRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(AiHordeSubmissionResult.Submitted("horde-123"));
        }
    }

    private sealed class FakeNamingUtilities : INamingUtilities
    {
        public string GetSubjectName() => "squirrel";

        public string GetSubjectSlug() => "squirrel";

        public string GetFileNamePrefix() => "squirrel";

        public string GetContainerName() => "squirrel-images";
    }

    private sealed class FakePromptBuilder : IPromptBuilder
    {
        public string BuildPrompt(string category) => $"Prompt for {category}";
    }
}
