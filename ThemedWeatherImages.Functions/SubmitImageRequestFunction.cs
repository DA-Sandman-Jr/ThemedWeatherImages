using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ThemedWeatherImages.Functions.Generation;

namespace ThemedWeatherImages.Functions;

public sealed class SubmitImageRequestFunction
{
    private readonly ImageGenerationService _generationService;
    private readonly INamingUtilities _namingUtilities;
    private readonly TimeProvider _timeProvider;

    public SubmitImageRequestFunction(
        ImageGenerationService generationService,
        INamingUtilities namingUtilities)
        : this(generationService, namingUtilities, TimeProvider.System)
    {
    }

    public SubmitImageRequestFunction(
        ImageGenerationService generationService,
        INamingUtilities namingUtilities,
        TimeProvider timeProvider)
    {
        _generationService = generationService;
        _namingUtilities = namingUtilities;
        _timeProvider = timeProvider;
    }

    [Function("SubmitImageRequestManual")]
    public async Task<HttpResponseData> RunManual(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "generate/{subject}")] HttpRequestData req,
        string? subject,
        FunctionContext context)
    {
        ILogger logger = context.GetLogger("SubmitImageRequestManual");
        string expectedSlug = _namingUtilities.GetSubjectSlug();
        ManualTriggerRequest manualRequest = await ManualTriggerRequest.ParseAsync(req, logger, expectedSlug, subject, _timeProvider, context.CancellationToken);

        if (!manualRequest.IsValid)
        {
            HttpResponseData badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync(manualRequest.ValidationError ?? "Invalid request payload.", context.CancellationToken);
            return badRequest;
        }

        IReadOnlyList<GenerationResult> generationResults = await _generationService.GenerateAsync(
            manualRequest.ToGenerationRequest(),
            context.CancellationToken);

        HttpResponseData response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ManualTriggerResponse.FromResults(
            manualRequest,
            _namingUtilities.GetSubjectName(),
            generationResults), cancellationToken: context.CancellationToken);

        return response;
    }
}
