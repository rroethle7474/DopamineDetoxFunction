using Azure;
using DopamineDetoxFunction.Models;
using DopamineDetoxFunction.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DopamineDetoxFunction
{
    public class SocialMediaRetrieverFunctions
    {
        private readonly ILogger<SocialMediaRetrieverFunctions> _logger;
        private readonly IYouTubeWrapperService _youTubeService;
        private readonly ITwitterService _twitterService;
        private readonly IDopamineDetoxApiService _dopamineDetoxApiService;
        private readonly ISignalRService _signalRService;


        public SocialMediaRetrieverFunctions(ILogger<SocialMediaRetrieverFunctions> logger,
            IYouTubeWrapperService youTubeService, ITwitterService twitterService, 
            IDopamineDetoxApiService dopamineDetoxApiService, ISignalRService signalRService)
        {
            _logger = logger;
            _youTubeService = youTubeService;
            _twitterService = twitterService;
            _dopamineDetoxApiService = dopamineDetoxApiService;
            _signalRService = signalRService;
        }

        [Function("WeeklyCleanupSocialMediaData")]
        public async Task<IActionResult> WeeklyCleanupSocialMediaData(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. WeeklyCleanupSocialMediaData");
            try
            {
                var socialMediaDataResponse = await CleanupSocialMediaData();
                return new OkObjectResult(socialMediaDataResponse);
            }
            catch(Exception e){
                return new OkObjectResult(e.Message);
            }
        }

        private async Task<SocialMediaDataResponse> CleanupSocialMediaData()
        {
            var socialMediaDataResponse = new SocialMediaDataResponse();
            bool isAlreadyRun = await _dopamineDetoxApiService.HasWeeklySearchResultReport(socialMediaDataResponse);
            if(isAlreadyRun)
            {
                if(socialMediaDataResponse.Errors.Count == 0)
                    socialMediaDataResponse.ReportMessage = "Weekly cleanup already run for today";
                else
                    socialMediaDataResponse.ReportMessage = "Errors determining if weekly status report has already been run. No cleanup performed.";

                return socialMediaDataResponse;
            }
            
            await ResetCache("true");
            var userList = await _dopamineDetoxApiService.GetMVPUserListAsync(socialMediaDataResponse);

            if (userList != null && userList.Any())
            {

                foreach (var user in userList)
                {
                    // add method to call api to send email to user with mvp results for week
                    await _dopamineDetoxApiService.EmailWeeklySearchReportByUser(user, socialMediaDataResponse);

                }
            }

            await _dopamineDetoxApiService.ClearMVPWeeklySearchResults(socialMediaDataResponse);
            await _dopamineDetoxApiService.AddWeeklySearchResultReport(socialMediaDataResponse);

            return socialMediaDataResponse;
        }


        [Function("GetXDefaultSearchResults")]
        public async Task<IActionResult> GetXDefaultSearchResults(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetXDefaultSearchResults");
            var socialMediaDataResponse = await RetrieveXDefaultData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }


        [Function("GetYTDefaultSearchResults")]
        public async Task<IActionResult> GetYTDefaultSearchResults([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetYTDefaultSearchResults");
            var socialMediaDataResponse = await RetrieveYouTubeDefaultData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }

        [Function("GetYTSocialMediaData")]
        public async Task<IActionResult> GetYTSocialMediaData(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetYTSocialMediaData");
            var socialMediaDataResponse = await RetrieveYouTubeData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }

        [Function("GetYTSocialMediaChannelData")]
        public async Task<IActionResult> GetYTSocialMediaChannelData(
       [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetYTSocialMediaChannelData");
            var socialMediaDataResponse = await RetrieveYouTubeChannelData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }

        [Function("GetXSocialMediaData")]
        public async Task<IActionResult> GetXSocialMediaData(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetXSocialMediaData");
            var socialMediaDataResponse = await RetrieveXData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }

        [Function("GetXSocialMediaChannelData")]
        public async Task<IActionResult> GetXSocialMediaChannelData(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. GetXSocialMediaChannelData");
            var socialMediaDataResponse = await RetrieveXChannelData(req.Query["isNew"]);
            return new OkObjectResult(socialMediaDataResponse);
        }

        [Function("SocialMediaDataHttpTrigger")]
        [SignalROutput(HubName = "socialmedia")]
        public async Task<SignalRMessageAction> GetSocialMediaDataHttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = new SocialMediaTimerDataResponse
            {
                Message = "Execution completed",
                ExecutionTime = DateTime.Now,
                NextExecutionTime = DateTime.Now.AddDays(1)
            };

            await ResetCache("true");

            try
            {
                response.DefaultYouTubeResponse = await RetrieveYouTubeDefaultData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube default data");
                response.Errors.Add($"Error retrieving YouTube default data: {ex.Message}");
            }

            try
            {
                response.DefaultXResponse = await RetrieveXDefaultData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X default data");
                response.Errors.Add($"Error retrieving X default data: {ex.Message}");
            }

            try
            {
                response.YouTubeResponse = await RetrieveYouTubeData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube data");
                response.Errors.Add($"Error retrieving YouTube data: {ex.Message}");
            }

            try
            {
                response.XResponse = await RetrieveXData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X data");
                response.Errors.Add($"Error retrieving X data: {ex.Message}");
            }

            try
            {
                response.YouTubeChannelResponse = await RetrieveYouTubeChannelData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube channel data");
                response.Errors.Add($"Error retrieving YouTube channel data: {ex.Message}");
            }

            try
            {
                response.XChannelResponse = await RetrieveXChannelData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X channel data");
                response.Errors.Add($"Error retrieving X channel data: {ex.Message}");
            }

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

            // Return the SignalR message directly
            return new SignalRMessageAction("dataUpdated", new[] { message });
        }

        // add the new SignalRMessageAction to send the dataUpdated message
        [Function("AddDailyQuoteHttpTrigger")]
        [SignalROutput(HubName = "socialmedia")]
        public async Task<SignalRMessageAction> AddDailyQuoteHttpTrigger([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request. AddDailyQuoteHttpTrigger");
            var isCreated = await AddDailySocialMediaQuote();
            var message = new
            {
                updateTime = DateTime.UtcNow,
                nextUpdateTime = DateTime.UtcNow.AddDays(1),
                hasErrors = isCreated,
            };
            return new SignalRMessageAction("dataUpdated", new[] { message });
        }

        #region TimerTriggers
        [Function("WeeklyCleanupSocialMediaDataTimerTrigger")]
        public async Task<IActionResult> WeeklyCleanupSocialMediaDataTimerTriggerRun(
    [TimerTrigger("%WeeklyCleanupTimerSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at: WeeklyCleanupSocialMediaDataTimerTrigger");
            try
            {
                var socialMediaDataResponse = await CleanupSocialMediaData();
                return new OkObjectResult(socialMediaDataResponse);
            }
            catch (Exception e)
            {
                return new OkObjectResult(e.Message);
            }
        }

        [Function("AddDailyQuoteTimerTrigger")]
        [SignalROutput(HubName = "socialmedia")]
        public async Task<SignalRMessageAction> AddDailyQuoteTimerTriggerRun([TimerTrigger("%DailyQuoteTimerSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation("C# Timer trigger function executed at for AddDailyQuoteTimer");
            var isCreated = await AddDailySocialMediaQuote();
            var message = new
            {
                updateTime = DateTime.UtcNow,
                nextUpdateTime = DateTime.UtcNow.AddDays(1),
                hasErrors = isCreated,
            };
            return new SignalRMessageAction("dataUpdated", new[] { message });
        }


        [Function("SocialMediaDataTimerRun")]
        public async Task<SignalRMessageAction> SocialMediaTimerRun(
            [TimerTrigger("%TimerSchedule%")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = new SocialMediaTimerDataResponse
            {
                Message = "Execution completed",
                ExecutionTime = DateTime.Now,
                NextExecutionTime = myTimer.ScheduleStatus?.Next
            };

            await ResetCache("true");

            try
            {
                response.DefaultYouTubeResponse = await RetrieveYouTubeDefaultData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube default data");
                response.Errors.Add($"Error retrieving YouTube default data: {ex.Message}");
            }

            try
            {
                response.DefaultXResponse = await RetrieveXDefaultData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X default data");
                response.Errors.Add($"Error retrieving X default data: {ex.Message}");
            }

            try
            {
                response.YouTubeResponse = await RetrieveYouTubeData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube data");
                response.Errors.Add($"Error retrieving YouTube data: {ex.Message}");
            }

            try
            {
                response.XResponse = await RetrieveXData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X data");
                response.Errors.Add($"Error retrieving X data: {ex.Message}");
            }

            try
            {
                response.YouTubeChannelResponse = await RetrieveYouTubeChannelData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving YouTube channel data");
                response.Errors.Add($"Error retrieving YouTube channel data: {ex.Message}");
            }

            try
            {
                response.XChannelResponse = await RetrieveXChannelData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving X channel data");
                response.Errors.Add($"Error retrieving X channel data: {ex.Message}");
            }

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

            // Return the SignalR message directly
            return new SignalRMessageAction("dataUpdated", new[] { message });
        }

        #endregion

        #region FunctionLogicMethods
        private async Task<SocialMediaDataResponse> RetrieveYouTubeData(string? isNew = "false")
        {
            var socialMediaDataResponse = new SocialMediaDataResponse();
            bool hasYouTubeReport = await _dopamineDetoxApiService.HasTodayYouTubeReport(isDefaultReport: false, isChannelReport: false);

            if (hasYouTubeReport)
            {
                socialMediaDataResponse.ReportMessage = "YouTube Data already retrieved successfully for today";
                return socialMediaDataResponse;
            }

            List<string> terms = new List<string>();
            await ResetCache(isNew);
            var yt_content_id = await _dopamineDetoxApiService.GetYouTubeContentTypeId();

            try
            {
                terms = (List<string>)await _dopamineDetoxApiService.GetSearchTerms();
            }
            catch
            {
                socialMediaDataResponse.Errors.Add("Error retrieving search terms from database.");
            }

            await _youTubeService.ProcessYouTubeSearchTerms(terms, socialMediaDataResponse);

            await _dopamineDetoxApiService.AddSearchResultReport(socialMediaDataResponse, yt_content_id);

            return socialMediaDataResponse;
        }

        private async Task<SocialMediaDataResponse> RetrieveYouTubeDefaultData(string? isNew = "false")
        {
            var defaultSocialMediaDataResponse = new SocialMediaDataResponse();
            bool hasYouTubeReport = await _dopamineDetoxApiService.HasTodayYouTubeReport(isDefaultReport: true, isChannelReport: false);
            if (hasYouTubeReport)
            {
                defaultSocialMediaDataResponse.ReportMessage = "Default YouTube Data already retrieved successfully for today";
                return defaultSocialMediaDataResponse;
            }

            var defaultTopics = new List<string>();
            await ResetCache(isNew);
            var yt_content_id = await _dopamineDetoxApiService.GetYouTubeContentTypeId();

            try
            {
                defaultTopics = await _dopamineDetoxApiService.GetDefaultTopics();
            }
            catch (Exception e)
            {
                defaultSocialMediaDataResponse.ReportMessage = "Error communicating with database to retrieve DefaultTopics.";
                defaultSocialMediaDataResponse.Errors.Add(e.Message);
                return defaultSocialMediaDataResponse;
            }

            if (defaultTopics == null || !defaultTopics.Any())
            {
                defaultSocialMediaDataResponse.ReportMessage = "No default topics found in database.";
                defaultSocialMediaDataResponse.Errors.Add("No default topics found in database.");
                return defaultSocialMediaDataResponse ;
            }

            await _youTubeService.ProcessYouTubeSearchTerms(defaultTopics, defaultSocialMediaDataResponse, isHomePage: true, isChannel: false);

            await _dopamineDetoxApiService.AddSearchResultReport(defaultSocialMediaDataResponse, yt_content_id, isDefaultReport: true, isChannelReport: false);

            return defaultSocialMediaDataResponse;
        }

        private async Task<SocialMediaDataResponse> RetrieveYouTubeChannelData(string? isNew = "false")
        {
            var socialMediaDataResponse = new SocialMediaDataResponse();
            bool hasYouTubeReport = await _dopamineDetoxApiService.HasTodayYouTubeReport(isDefaultReport: false, isChannelReport: true);
            if (hasYouTubeReport)
            {
                socialMediaDataResponse.ReportMessage = "Channel YouTube Data already retrieved successfully for today";
                return socialMediaDataResponse;
            }

            await ResetCache(isNew);
            var yt_content_id = await _dopamineDetoxApiService.GetYouTubeContentTypeId();

            var youTubeChannelIdentifiers = await _dopamineDetoxApiService.GetYouTubeChannels();
            if (youTubeChannelIdentifiers != null && youTubeChannelIdentifiers.Any())
            {
                foreach (var channel in youTubeChannelIdentifiers)
                {
                    if (System.String.IsNullOrEmpty(channel))
                    {
                        continue;
                    }
                    try
                    {
                        var (channelArticles, errors) = await _youTubeService.GetYouTubeVideosByHandleAsync(channel);
                        if (channelArticles != null && channelArticles.Any())
                        {
                            await _youTubeService.SaveYTResultsAsync(channelArticles, socialMediaDataResponse, channel, isHomePage: false, isChannel: true);
                        }
                        else
                        {
                            socialMediaDataResponse.ReportMessage = "No YouTube channel results found.";
                        }
                    }
                    catch (Exception e)
                    {
                        socialMediaDataResponse.Errors.Add($"Error adding channel results for {channel}. {e.Message}");
                        continue;
                    }
                }
            }

            await _dopamineDetoxApiService.AddSearchResultReport(socialMediaDataResponse, yt_content_id, false, true);
            return socialMediaDataResponse;
        }

        private async Task<SocialMediaDataResponse> RetrieveXData(string? isNew = "false")
        {
            var socialMediaDataResponse = new SocialMediaDataResponse();
            bool hasTwitterReport = await _dopamineDetoxApiService.HasTodayTwitterReport(isDefaultReport: false, isChannelReport: false);

            if (hasTwitterReport)
            {
                socialMediaDataResponse.ReportMessage = "Twitter Data already retrieved successfully for today";
                return socialMediaDataResponse;
            }

            List<string> terms = new List<string>();
            await ResetCache(isNew);
            var twitter_content_id = await _dopamineDetoxApiService.GetTwitterContentTypeId();

            try
            {
                terms = (List<string>)await _dopamineDetoxApiService.GetSearchTerms(true);
            }
            catch
            {
                socialMediaDataResponse.Errors.Add("Error retrieving search terms from database. Twitter.");
            }

            if (terms != null && terms.Any())
            {
                try
                {
                    var twitterArticles = await _twitterService.GetTwitterResultsAsync(terms, socialMediaDataResponse);
                    if (twitterArticles != null && twitterArticles.Any())
                    {
                        await _twitterService.SaveTwitterResultsAsync(twitterArticles, socialMediaDataResponse);
                    }
                }
                catch (Exception e)
                {
                    socialMediaDataResponse.Errors.Add(e.Message);
                }
            }

            await _dopamineDetoxApiService.AddSearchResultReport(socialMediaDataResponse, twitter_content_id);
            return socialMediaDataResponse;
        }

        private async Task<SocialMediaDataResponse> RetrieveXDefaultData(string? isNew = "false")
        {
            var defaultSocialMediaDataResponse = new SocialMediaDataResponse();
            bool hasTwitterReport = await _dopamineDetoxApiService.HasTodayTwitterReport(isDefaultReport: true, isChannelReport: false);
            if (hasTwitterReport)
            {
                defaultSocialMediaDataResponse.ReportMessage = "Default Twitter Data already retrieved successfully for today";
                return defaultSocialMediaDataResponse;
            }

            var defaultTopics = new List<string>();
            await ResetCache(isNew);
            var twitter_content_id = await _dopamineDetoxApiService.GetTwitterContentTypeId();

            try
            {
                defaultTopics = await _dopamineDetoxApiService.GetDefaultTopics(true);
            }
            catch (Exception e)
            {
                defaultSocialMediaDataResponse.ReportMessage = "Error communicating with database to retrieve Default X Topics.";
                defaultSocialMediaDataResponse.Errors.Add(e.Message);
                return defaultSocialMediaDataResponse;
            }

            try
            {
                var twitterArticles = await _twitterService.GetTwitterResultsAsync(defaultTopics, defaultSocialMediaDataResponse, isDefaultSearch: true);

                if (twitterArticles != null && twitterArticles.Any())
                {
                    await _twitterService.SaveTwitterResultsAsync(twitterArticles, defaultSocialMediaDataResponse, isHomePage: true);
                }
            }
            catch (Exception e)
            {
                defaultSocialMediaDataResponse.Errors.Add(e.Message);
            }

            await _dopamineDetoxApiService.AddSearchResultReport(defaultSocialMediaDataResponse, twitter_content_id, true);

            return defaultSocialMediaDataResponse;
        }

        private async Task<SocialMediaDataResponse> RetrieveXChannelData(string? isNew = "false")
        {
            var socialMediaDataResponse = new SocialMediaDataResponse();
            bool hasTwitterReport = await _dopamineDetoxApiService.HasTodayTwitterReport(isDefaultReport: false, isChannelReport: true);

            if (hasTwitterReport)
            {
                socialMediaDataResponse.ReportMessage = "Twitter Channel Data already retrieved successfully for today";
                return socialMediaDataResponse;
            }

            await ResetCache(isNew);
            var twitter_content_id = await _dopamineDetoxApiService.GetTwitterContentTypeId();

            var twitterChannelIdentifiers = await _dopamineDetoxApiService.GetTwitterChannels();
            if (twitterChannelIdentifiers != null && twitterChannelIdentifiers.Count() > 0)
            {
                var twitter_channel_articles = await _twitterService.GetTwitterResultsAsync(twitterChannelIdentifiers, socialMediaDataResponse, isChannelSearch: true);
                if (twitter_channel_articles != null && twitter_channel_articles.Any())
                {
                    await _twitterService.SaveTwitterResultsAsync(twitter_channel_articles, socialMediaDataResponse, isChannelResult: true);
                }
            }
            else
            {
                socialMediaDataResponse.Errors.Add("No Twitter channels found in database.");
                socialMediaDataResponse.ReportMessage = "No Twitter channels found in database.";
            }

            await _dopamineDetoxApiService.AddSearchResultReport(socialMediaDataResponse, twitter_content_id, isChannelReport: true);

            return socialMediaDataResponse;
        }

        private async Task<bool> AddDailySocialMediaQuote()
        {
            var isCreated = await _dopamineDetoxApiService.CreateDailyQuote();
            
            return isCreated;
        }


        #endregion

        #region Helper Methods
        private async Task ResetCache(string? resetApiCache)
        {
            bool isNew = bool.TryParse(resetApiCache, out bool parsedIsNew) ? parsedIsNew : false;

            if (isNew)
            {
                await _dopamineDetoxApiService.ClearSearchResults();
                //await _twitterService.ClearSearchResults();
            }
        }
        #endregion
    }
}
