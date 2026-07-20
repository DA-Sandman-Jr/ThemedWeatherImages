using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions;

public sealed class BudgetKillSwitchFunction
{
    private readonly IScheduledImageRequestControlStore _controlStore;
    private readonly TimeProvider _timeProvider;

    public BudgetKillSwitchFunction(IScheduledImageRequestControlStore controlStore)
        : this(controlStore, TimeProvider.System)
    {
    }

    public BudgetKillSwitchFunction(IScheduledImageRequestControlStore controlStore, TimeProvider timeProvider)
    {
        _controlStore = controlStore;
        _timeProvider = timeProvider;
    }

    [Function("DisableScheduledImageRequestBudgetKillSwitch")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "budget/disable-scheduled-image-request")] HttpRequestData req,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("DisableScheduledImageRequestBudgetKillSwitch");

        try
        {
            DateTimeOffset disabledAt = _timeProvider.GetUtcNow();
            DateTimeOffset disabledUntil = GetStartOfNextUtcMonth(disabledAt);

            await _controlStore.DisableUntilAsync(disabledAt, disabledUntil, context.CancellationToken);
            logger.BudgetKillSwitchDisabled(disabledUntil);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Scheduled image generation is disabled until {disabledUntil:O}.", context.CancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            logger.BudgetKillSwitchFailed(ex);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Budget kill switch failed.", context.CancellationToken);
            return response;
        }
    }

    internal static DateTimeOffset GetStartOfNextUtcMonth(DateTimeOffset now)
    {
        DateTimeOffset utcNow = now.ToUniversalTime();
        int year = utcNow.Year;
        int month = utcNow.Month + 1;

        if (month == 13)
        {
            month = 1;
            year++;
        }

        return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
