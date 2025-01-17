using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DopamineDetoxFunction
{
    public class SocialMediaMessageFunctions
    {
        private readonly ILogger<SocialMediaMessageFunctions> _logger;

        public SocialMediaMessageFunctions(ILogger<SocialMediaMessageFunctions> logger)
        {
            _logger = logger;
        }

        [Function("negotiate")]
        public async Task<HttpResponseData> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "socialmedia")] string connectionInfo)
        {
            _logger.LogInformation("Negotiating SignalR connection");
            _logger.LogInformation(connectionInfo);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(connectionInfo);
            return response;
        }

        [Function("SendUpdate")]
        [SignalROutput(HubName = "socialmedia")]
        public SignalRMessageAction SendUpdate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            //var response = // your SocialMediaTimerDataResponse object

            var message = new
            {
                updateTime = DateTime.Now,
                nextUpdateTime = DateTime.Now.AddDays(7),
            };

            return new SignalRMessageAction(
                "dataUpdated",
                new[] { message }
            );
        }
    }
}