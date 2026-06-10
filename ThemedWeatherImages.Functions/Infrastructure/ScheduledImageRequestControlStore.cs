using Azure;
using Azure.Data.Tables;

namespace ThemedWeatherImages.Functions.Infrastructure;

public interface IScheduledImageRequestControlStore
{
    Task DisableUntilAsync(DateTimeOffset disabledAt, DateTimeOffset disabledUntil, CancellationToken cancellationToken);

    Task<bool> IsDisabledAsync(DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class ScheduledImageRequestControlStore : IScheduledImageRequestControlStore
{
    private const string PartitionKeyValue = "controls";
    private const string RowKeyValue = "scheduledImageRequests";

    private readonly TableClient _table;

    public ScheduledImageRequestControlStore(string storageConnectionString, string tableName = "themedWeatherImagesControls")
    {
        _table = new TableClient(storageConnectionString, tableName);
    }

    public async Task DisableUntilAsync(
        DateTimeOffset disabledAt,
        DateTimeOffset disabledUntil,
        CancellationToken cancellationToken)
    {
        await _table.CreateIfNotExistsAsync(cancellationToken);

        var entity = new ScheduledImageRequestControlEntity
        {
            IsDisabled = true,
            DisabledAt = disabledAt,
            DisabledUntil = disabledUntil,
            DisabledReason = "Azure Cost Management budget alert"
        };

        await _table.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> IsDisabledAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            Response<ScheduledImageRequestControlEntity> response = await _table.GetEntityAsync<ScheduledImageRequestControlEntity>(
                PartitionKeyValue,
                RowKeyValue,
                cancellationToken: cancellationToken);

            ScheduledImageRequestControlEntity entity = response.Value;
            if (!entity.IsDisabled)
            {
                return false;
            }

            return !entity.DisabledUntil.HasValue || entity.DisabledUntil.Value > now;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    private sealed class ScheduledImageRequestControlEntity : ITableEntity
    {
        public DateTimeOffset? DisabledAt { get; set; }

        public string? DisabledReason { get; set; }

        public DateTimeOffset? DisabledUntil { get; set; }

        public ETag ETag { get; set; }

        public bool IsDisabled { get; set; }

        public string PartitionKey { get; set; } = PartitionKeyValue;

        public string RowKey { get; set; } = RowKeyValue;

        public DateTimeOffset? Timestamp { get; set; }
    }
}
