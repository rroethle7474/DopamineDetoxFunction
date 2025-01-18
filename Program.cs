using DopamineDetox.ServiceAgent.Extensions;
using DopamineDetoxFunction.Services;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults(workerApplication =>
    {
        workerApplication.UseFunctionExecutionMiddleware();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddMemoryCache(); // Add this line to register IMemoryCache

        // add YouTubeService & Wrapper with HttpClient
        var youTubeApiKey = configuration["YouTubeApiKey"];
        var searchApiUrl = configuration["SearchApiUrl"] ?? "http://127.0.0.1:5000";
        var oEmbedApiUrl = configuration["XOEmbedApiUrl"] ?? "https://publish.twitter.com/oembed?url=";

        services.AddHttpClient<ITwitterEmbedService, TwitterEmbedService>(client =>
        {
            client.BaseAddress = new Uri(oEmbedApiUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // python web scraper to get twitter results
        services.AddHttpClient<ITwitterService, TwitterService>(client =>
        {
            client.BaseAddress = new Uri(searchApiUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient();
        services.AddTransient(provider =>
        {
            return new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youTubeApiKey,
                HttpClientFactory = new Google.Apis.Http.HttpClientFactory()
            });
        });

        services.AddTransient<IYouTubeWrapperService, YouTubeWrapperService>();

        var azureSignalRConnectionString = configuration["AzureSignalRConnectionString"];
        services.AddSignalR().AddAzureSignalR(azureSignalRConnectionString);
        services.AddTransient<ISignalRService, SignalRService>();

        var baseUrl = configuration["DopamineDetox:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("DopamineDetox:BaseUrl configuration is missing or empty.");
        }

        services.AddDopamineDetoxServiceAgent(options =>
        {
            options.BaseUrl = baseUrl;
            options.TimeoutSeconds = int.Parse(configuration["DopamineDetox:TimeoutSeconds"] ?? "30");
            options.MaxRetryAttempts = int.Parse(configuration["DopamineDetox:MaxRetryAttempts"] ?? "3");
            options.RetryDelayMilliseconds = int.Parse(configuration["DopamineDetox:RetryDelayMilliseconds"] ?? "1000");
        });

        services.AddTransient<IDopamineDetoxApiService, DopamineDetoxApiService>();
    })
    .Build();

host.Run();