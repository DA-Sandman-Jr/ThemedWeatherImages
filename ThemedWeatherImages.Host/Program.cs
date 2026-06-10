using ThemedWeatherImages;

// Reference host for the ThemedWeatherImages read-side API. Consumers copy this
// shape into their own ASP.NET Core host and supply their theme; the controllers
// come from the referenced library. Set WeatherApi:ApiKey via user secrets:
//   dotnet user-secrets set "WeatherApi:ApiKey" "<your-weatherapi-key>"
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddThemedWeatherImages(options =>
{
    options.Theme.DisplayName = builder.Configuration["ThemedWeatherImages:Theme:DisplayName"];
    options.Theme.SubjectName = builder.Configuration["ThemedWeatherImages:Theme:SubjectName"];
    options.Theme.SubjectSlug = builder.Configuration["ThemedWeatherImages:Theme:SubjectSlug"];
    options.Theme.ImageFileNamePrefix = builder.Configuration["ThemedWeatherImages:Theme:ImageFileNamePrefix"];
    options.WeatherApi.ApiKey = builder.Configuration["WeatherApi:ApiKey"];
    options.WeatherApi.DefaultIp = builder.Configuration["WeatherApi:DefaultIp"];
    options.WeatherApi.ExposeErrorDiagnostics =
        bool.TryParse(builder.Configuration["WeatherApi:ExposeErrorDiagnostics"], out bool exposeErrorDiagnostics)
        && exposeErrorDiagnostics;
    options.Images.BlobBaseUrl = builder.Configuration["WeatherImages:BlobBaseUrl"];
    options.Images.BlobSasToken = builder.Configuration["WeatherImages:BlobSasToken"];
});

WebApplication app = builder.Build();

app.MapControllers();

app.Run();
