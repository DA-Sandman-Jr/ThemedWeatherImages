using Azure.Storage.Blobs;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions.Generation;

public interface IGeneratedImageStore
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);

    Task<bool> ImageExistsAsync(string fileName, CancellationToken cancellationToken);

    Task SaveRequestMappingAsync(string hordeId, string fileName, CancellationToken cancellationToken);
}

public sealed class BlobGeneratedImageStore : IGeneratedImageStore
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly HordeRequestMappingStore _mappingStore;
    private readonly INamingUtilities _namingUtilities;

    public BlobGeneratedImageStore(
        BlobServiceClient blobServiceClient,
        HordeRequestMappingStore mappingStore,
        INamingUtilities namingUtilities)
    {
        _blobServiceClient = blobServiceClient;
        _mappingStore = mappingStore;
        _namingUtilities = namingUtilities;
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        await _mappingStore.EnsureTableExistsAsync(cancellationToken);
        await GetContainerClient().CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ImageExistsAsync(string fileName, CancellationToken cancellationToken)
    {
        BlobClient blobClient = GetContainerClient().GetBlobClient(fileName);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task SaveRequestMappingAsync(string hordeId, string fileName, CancellationToken cancellationToken)
    {
        await _mappingStore.SaveMappingAsync(hordeId, fileName, cancellationToken);
    }

    private BlobContainerClient GetContainerClient() =>
        _blobServiceClient.GetBlobContainerClient(_namingUtilities.GetContainerName());
}
