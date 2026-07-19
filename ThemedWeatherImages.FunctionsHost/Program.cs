using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThemedWeatherImages;
using ThemedWeatherImages.Functions.Domain;
using ThemedWeatherImages.Functions.Generation;
using ThemedWeatherImages.Functions.Infrastructure;

// Reference host for the ThemedWeatherImages.Functions library. Consumers copy
// this shape into their own Functions host project and supply their theme via
// configuration; the function classes themselves live in the referenced library.
IHost host = Host.CreateDefaultBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        string blobConnString = config["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");

        if (!ImageHostAllowList.TryCreate(config["AIHORDE_ALLOWED_IMAGE_HOSTS"], out ImageHostAllowList? allowList, out string? allowListError))
        {
            throw new InvalidOperationException($"Invalid AIHORDE_ALLOWED_IMAGE_HOSTS: {allowListError}");
        }

        services.AddSingleton(allowList!);
        services.AddSingleton(new BlobServiceClient(blobConnString));
        services.AddSingleton(new HordeRequestMappingStore(blobConnString));
        services.AddSingleton<IScheduledImageRequestControlStore>(new ScheduledImageRequestControlStore(blobConnString));
        services.AddSingleton<IGeneratedImageStore, BlobGeneratedImageStore>();
        services.AddSingleton<IAiHordeClient, AiHordeClient>();
        services.AddSingleton<ImageGenerationService>();

        services.AddHttpClient();
        services.AddThemedWeatherImageGenerationSupport(options =>
        {
            options.Theme.DisplayName = config["ThemedWeatherImages:Theme:DisplayName"];
            options.Theme.SubjectName = config["ThemedWeatherImages:Theme:SubjectName"];
            options.Theme.SubjectSlug = config["ThemedWeatherImages:Theme:SubjectSlug"];
            options.Theme.ImageFileNamePrefix = config["ThemedWeatherImages:Theme:ImageFileNamePrefix"];
            options.Images.BlobContainerName = config["ThemedWeatherImages:Images:BlobContainerName"];
            options.Generation.PromptTemplate = config["ThemedWeatherImages:Generation:PromptTemplate"];
            options.Generation.Model = config["ThemedWeatherImages:Generation:Model"] ?? options.Generation.Model;
            options.Generation.CfgScale = config.GetValue<double?>("ThemedWeatherImages:Generation:CfgScale") ?? options.Generation.CfgScale;
            options.Generation.SamplerName = config["ThemedWeatherImages:Generation:SamplerName"] ?? options.Generation.SamplerName;
            options.Generation.Width = config.GetValue<int?>("ThemedWeatherImages:Generation:Width") ?? options.Generation.Width;
            options.Generation.Height = config.GetValue<int?>("ThemedWeatherImages:Generation:Height") ?? options.Generation.Height;
        });
    })
    .Build();

await host.RunAsync();
