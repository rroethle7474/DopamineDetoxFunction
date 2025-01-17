using DopamineDetoxFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DopamineDetoxFunction.Services
{

    public class SignalRService : ISignalRService
    {
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(ILogger<SignalRService> logger)
        {
            _logger = logger;
        }

        public SignalRMessageAction CreateDataUpdateNotification(SocialMediaTimerDataResponse response)
        {
            try
            {
                var message = new
                {
                    updateTime = response.ExecutionTime,
                    nextUpdateTime = response.NextExecutionTime,
                    hasErrors = response.Errors.Any(),
                    summary = new
                    {
                        defaultYouTube = new
                        {
                            success = response.DefaultYouTubeResponse.SuccessResults,
                            errors = response.DefaultYouTubeResponse.Errors.Count,
                            message = response.DefaultYouTubeResponse.ReportMessage
                        },
                        defaultX = new
                        {
                            success = response.DefaultXResponse.SuccessResults,
                            errors = response.DefaultXResponse.Errors.Count,
                            message = response.DefaultXResponse.ReportMessage
                        },
                        youtube = new
                        {
                            success = response.YouTubeResponse.SuccessResults,
                            errors = response.YouTubeResponse.Errors.Count,
                            message = response.YouTubeResponse.ReportMessage
                        },
                        x = new
                        {
                            success = response.XResponse.SuccessResults,
                            errors = response.XResponse.Errors.Count,
                            message = response.XResponse.ReportMessage
                        },
                        youtubeChannel = new
                        {
                            success = response.YouTubeChannelResponse.SuccessResults,
                            errors = response.YouTubeChannelResponse.Errors.Count,
                            message = response.YouTubeChannelResponse.ReportMessage
                        },
                        xChannel = new
                        {
                            success = response.XChannelResponse.SuccessResults,
                            errors = response.XChannelResponse.Errors.Count,
                            message = response.XChannelResponse.ReportMessage
                        }
                    }
                };

                return new SignalRMessageAction("dataUpdated", new[] { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SignalR notification");
                throw;
            }
        }

        public SignalRMessageAction CreateQuoteDataUpdateNotification(bool hasQuoteErrors = false)
        {
            try
            {
                var message = new
                {
                    updateTime = DateTime.UtcNow,
                    nextUpdateTime = DateTime.UtcNow.AddDays(1),
                    hasErrors = hasQuoteErrors
                };

                return new SignalRMessageAction("dataUpdated", new[] { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SignalR notification");
                throw;
            }
        }

        //public async Task SendMessageAsync(string message)
        //{
        //    try
        //    {
        //        await _hubContext.Clients.All.SendAsync("dataUpdated", message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error sending SignalR message");
        //        throw;
        //    }
        //}
    }
}