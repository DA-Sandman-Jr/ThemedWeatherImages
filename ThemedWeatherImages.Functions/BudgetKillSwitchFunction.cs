using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Infrastructure;

namespace ThemedWeatherImages.Functions;

public sealed class BudgetKillSwitchFunction
{
    private readonly IScheduledImageRequestControlStore _controlStore;

    public BudgetKillSwitchFunction(IScheduledImageRequestControlStore controlStore)
    {
        _controlStore = controlStore;
    }

    [Function("DisableScheduledImageRequestBudgetKillSwitch")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "budget/disable-scheduled-image-request")] HttpRequestData req,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("DisableScheduledImageRequestBudgetKillSwitch");

        try
        {
            DateTimeOffset disabledAt = DateTimeOffset.UtcNow;
            DateTimeOffset disabledUntil = GetStartOfNextUtcMonth(disabledAt);

            await _controlStore.DisableUntilAsync(disabledAt, disabledUntil, context.CancellationToken);
            logger.LogWarning(
                "Budget kill switch disabled scheduled image generation until {DisabledUntil}.",
                disabledUntil);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Scheduled image generation is disabled until {disabledUntil:O}.");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Budget kill switch failed to disable scheduled image generation.");

            HttpResponseData response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Budget kill switch failed.");
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
