#nullable enable

using Azure;
using Azure.Data.Tables;

namespace ThemedWeatherImages.Functions.Infrastructure;

public class HordeRequestMapping : ITableEntity
{
    public ETag ETag { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = "horde";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
}

public class HordeRequestMappingStore
{
    private readonly TableClient _table;

    public HordeRequestMappingStore(string storageConnectionString, string tableName = "hordeRequestMappings")
    {
        _table = new TableClient(storageConnectionString, tableName);
    }

    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default) =>
        await _table.CreateIfNotExistsAsync(cancellationToken);

    public async Task<string?> GetFilenameByHordeIdAsync(string hordeId, CancellationToken cancellationToken = default)
    {
        try
        {
            Response<HordeRequestMapping> response = await _table.GetEntityAsync<HordeRequestMapping>("horde", hordeId, cancellationToken: cancellationToken);
            return response.Value.Filename;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task SaveMappingAsync(string hordeId, string filename, CancellationToken cancellationToken = default)
    {
        var entity = new HordeRequestMapping
        {
            RowKey = hordeId,
            Filename = filename
        };

        await _table.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }
}
