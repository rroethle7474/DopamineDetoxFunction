using DopamineDetox.ServiceAgent.Extensions;
using DopamineDetoxFunction.Services;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Added for logging

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging((hostingContext, logging) => // Added logging configuration
    {
        // Add console logging for local development and Azure Log Stream
        logging.AddConsole();

        // Set default log level
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((context, services) =>
    {
        try // Added error handling for service configuration
        {
            var configuration = context.Configuration;
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();

            services.AddMemoryCache();

            // YouTube Service configuration
            var youTubeApiKey = configuration["YouTubeApiKey"];
            var searchApiUrl = configuration["SearchApiUrl"] ?? "http://127.0.0.1:5000";
            var oEmbedApiUrl = configuration["XOEmbedApiUrl"] ?? "https://publish.twitter.com/oembed?url=";

            services.AddHttpClient<ITwitterEmbedService, TwitterEmbedService>(client =>
            {
                client.BaseAddress = new Uri(oEmbedApiUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

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
        }
        catch (Exception ex)
        {
            // Log configuration errors to console before Application Insights is initialized
            Console.WriteLine($"FATAL ERROR during service configuration: {ex}");
            throw;
        }
    })
    .Build();

try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Function host initializing...");

    host.Run();
}
catch (Exception ex)
{
    // Log any host initialization errors
    Console.WriteLine($"Host initialization failed: {ex}");
    throw;
}