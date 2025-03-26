using DopamineDetox.Domain.Dtos;
using DopamineDetox.ServiceAgent.Interfaces;
using DopamineDetox.ServiceAgent.Requests;
using DopamineDetoxFunction.Extensions;
using DopamineDetoxFunction.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DopamineDetoxFunction.Services
{
    public class DopamineDetoxApiService : IDopamineDetoxApiService
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IChannelService _channelService;
        private readonly IDefaultTopicService _defaultTopicService;
        private readonly INoteService _noteService;
        private readonly IQuoteService _quoteService;
        private readonly IResetService _resetService;
        private readonly ISearchResultService _searchResultService;
        private readonly ISearchResultReportService _searchResultReportService;
        private readonly ISubTopicService _subTopicService;
        private readonly ITopicService _topicService;
        private readonly ITopSearchResultService _topSearchResultService;
        private readonly IWeeklySearchResultReportService _weeklySearchResultReportService;
        private readonly IMemoryCache _cache;

        private readonly string YOU_TUBE_CATEGORY = "youtube"; // 3
        private readonly string TWITTER_CATEGORY = "twitter/x"; // 2

        public DopamineDetoxApiService(IContentTypeService contentTypeService, IChannelService channelService, IDefaultTopicService defaultTopicService,   INoteService noteService, IQuoteService quoteService, IResetService resetService,
            ISearchResultService searchResultService, ISearchResultReportService searchResultReportService, ISubTopicService subTopicService, 
            ITopicService topicService, ITopSearchResultService topSearchResultService,
            IWeeklySearchResultReportService weeklySearchResultReportService, IMemoryCache cache)
        {
            _contentTypeService = contentTypeService;
            _channelService = channelService;
            _defaultTopicService = defaultTopicService;
            _noteService = noteService;
            _quoteService = quoteService;
            _resetService = resetService;
            _searchResultService = searchResultService;
            _searchResultReportService = searchResultReportService;
            _subTopicService = subTopicService;
            _topicService = topicService;
            _topSearchResultService = topSearchResultService;
            _weeklySearchResultReportService = weeklySearchResultReportService;
            _cache = cache;
        }

        #region ContentTypes
        public async Task<IEnumerable<ContentTypeDto>> GetContentTypes()
        {
            const string cacheKey = "ContentTypes";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<ContentTypeDto> cachedContentTypes))
            {
                return cachedContentTypes;
            }

            var reportTypesData = await _contentTypeService.GetContentTypesAsync(new CancellationToken());
            if (!reportTypesData.Success || reportTypesData?.Data == null || !reportTypesData.Data.Any())
            {
                throw new Exception($"Error getting content types. {reportTypesData?.Message}");
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, reportTypesData.Data, cacheEntryOptions);

            return reportTypesData.Data;
        }

        public async Task<int> GetYouTubeContentTypeId()
        {
            return (await GetContentTypes()).FirstOrDefault(ct => ct.Title?.ToLower() == YOU_TUBE_CATEGORY)?.Id ?? 0;
        }

        public async Task<int> GetTwitterContentTypeId()
        {
            return (await GetContentTypes()).FirstOrDefault(ct => ct.Title?.ToLower() == TWITTER_CATEGORY)?.Id ?? 0;
        }

        #endregion

        #region SearchResults
        public async Task<IEnumerable<string>> GetSearchTerms(bool excludeTwitter = false)
        {
            const string cacheKey = "SearchTerms";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<string> cachedSearchTerms))
            {
                return cachedSearchTerms;
            }


            GetSubTopicsRequest stRequest = new GetSubTopicsRequest
            {
                IsActive = true,
            };

            GetTopicsRequest tRequest = new GetTopicsRequest
            {
                IsActive = true,
            };

            List<string> terms = new List<string>();

            try
            {
                var subTopicData = await _subTopicService.GetSubTopicsAsync(stRequest, new CancellationToken());

                if(excludeTwitter)
                {
                    subTopicData.Data = subTopicData?.Data?.Where(st => st.ExcludeFromTwitter != true).ToList();
                }

                var subTopics = subTopicData?.Data?
                               .GroupBy(st => st.Term?.Trim().ToLower())
                               .Select(g => g.First())
                               .ToList();

                var topicData = await _topicService.GetTopicsAsync(tRequest, new CancellationToken());

                if (excludeTwitter)
                {
                    topicData.Data = topicData?.Data?.Where(t => t.ExcludeFromTwitter != true).ToList();
                }

                var topics = topicData?.Data?.GroupBy(t => t.Term?.Trim().ToLower())
                .Select(g => g.First())
                .ToList();

                var subTopicTerms = subTopics?.Select(st => st.Term.Trim().ToLower()).ToList();
                var topicTerms = topics?.Select(t => t.Term.Trim().ToLower()).ToList();
                terms = subTopicTerms?.Union(topicTerms ?? new List<string>()).ToList() ?? new List<string>();
            }
            catch(Exception e)
            {
                throw new Exception($"Error getting search terms. {e.Message}");
            }


            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, terms, cacheEntryOptions);

            return terms;
        }

        public async Task<SearchResultsResponseDto> AddMultipleSearchResultsAsync(IEnumerable<SearchResultDto> searchResults)
        {
            var response = await _searchResultService.AddMultipleSearchResultsAsync(searchResults, new CancellationToken());
            if(response?.Data == null)
            {
                throw new Exception("Error saving multiple search results. Response/Data returned is null");
            }

            if (response.Success == false)
            {
                throw new Exception($"Error saving multiple search results. {response.Message}");
            }

            return response.Data;
        }

        public async Task CreateWeeklySearchReportRecord()
        {
            var weekelyReport = new WeeklySearchResultReportDto
            {
                ReportDate = DateTime.Now,
                IsSuccess = false,
                CreatedOn = DateTime.Now
            };


        }

        public async Task ClearMVPWeeklySearchResults(SocialMediaDataResponse response)
        {
            var clearDate = DateTime.Now;
            try
            {
                await _topSearchResultService.DeleteTopSearchResultsByDate(clearDate, true,new CancellationToken());
                await _noteService.DeleteNotesByDate(clearDate, true,new CancellationToken());
                await _searchResultService.DeleteSearchResultsByDate(clearDate, true,new CancellationToken());
                await _searchResultReportService.DeleteSearchResultReportsByDate(clearDate, true, new CancellationToken());
                response.ReportMessage = "Successfully cleared MVP weekly search results";
            }
            catch (Exception e)
            {
                response.Errors.Add(e.Message);
                response.ReportMessage = "Error clearing MVP weekly search results";
            }
        }

        public async Task<IEnumerable<string>> GetMVPUserListAsync(SocialMediaDataResponse response)
        {
            var userList = new List<string>();
            try
            {
                var noteUsers = await _noteService.GetNotesUserList(new CancellationToken());
                var topSearchResultsUsers = await _topSearchResultService.GetTopSearchResultsNoteList(new CancellationToken());

                if (noteUsers?.Data != null && noteUsers.Data.Any())
                {
                    userList.AddRange(noteUsers.Data);
                }

                if (topSearchResultsUsers?.Data != null && topSearchResultsUsers.Data.Any())
                {
                    userList.AddRange(topSearchResultsUsers.Data);
                }

                userList = userList.Distinct().ToList();
                response.ReportMessage = "Successfully got MVP user list";
            }
            catch(Exception e)
            {
                response.Errors.Add(e.Message);
                response.ReportMessage = "Error getting MVP user list";
            }

            return userList;
        }

        public async Task<bool> HasWeeklySearchResultReport(SocialMediaDataResponse response)
        {
            try
            {
                var request = new GetWeeklySearchResultReportsRequest
                {
                    IsSuccess = true
                };
                var reports = await _weeklySearchResultReportService.GetWeeklySearchResultReportsAsync(request, new CancellationToken());
                if(reports?.Data == null)
                {
                    throw new Exception("Error getting weekly search result reports. Data is null");
                }
                if(reports.Data.Any())
                {
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                response.Errors.Add(e.Message);
                // returning true to stop the process as something is probably wrong with the api/database if we are encountering an error
                return true;
            }
        }

        public async Task<bool> AddWeeklySearchResultReport(SocialMediaDataResponse response)
        {
            try
            {
                var weekelyReport = new WeeklySearchResultReportDto
                {
                    ReportDate = DateTime.Now,
                    IsSuccess = response.Errors.Any() == false,
                    CreatedOn = DateTime.Now,
                    ErrorMessage= response?.Errors?.FormatErrorMessages()
                };

                var result = await _weeklySearchResultReportService.CreateWeeklySearchResultReportAsync(weekelyReport, new CancellationToken());

                if (!result.Success)
                {
                    response.Errors.Add($"Error saving weekly search result report. {result.Message}");
                    response.ReportMessage = "Error saving weekly search result report";
                }
                else
                {
                    response.ReportMessage = "Successfully saved weekly search result report";
                }
                return result.Success;
            }
            catch (Exception e)
            {
                response.Errors.Add(e.Message);
                response.ReportMessage = "Error saving weekly search result report";
                return false;
            }
        }

        public async Task<bool> EmailWeeklySearchReportByUser(string userId, SocialMediaDataResponse response)
        {
            if (System.String.IsNullOrEmpty(userId))
            {
                return false;
            }

            try
            {
                var isSuccess = await _weeklySearchResultReportService.EmailUserWeeklyReport(userId, new CancellationToken());
                if (isSuccess?.Data == null )
                {
                    response.Errors.Add($"Error emailing weekly search result report. {userId}");
                    return false;
                }

                if (!isSuccess.Success || !isSuccess.Data)
                {
                    response.Errors.Add($"Error emailing weekly search result report. {userId}");
                }
                return isSuccess.Data;
            }
            catch(Exception e)
            {
                response.Errors.Add(e.Message);
                return false;
            }
        }

            #endregion


            #region SearchReportResultsRegion

            public async Task<bool> HasTodayYouTubeReport(bool? isDefaultReport = null, bool? isChannelReport = null)
        {
            var youTubeContentTypeId = await GetYouTubeContentTypeId();
            GetSearchResultReportRequest srrRequest = new GetSearchResultReportRequest
            {
                IsSuccess = true,
                To = DateTime.Today.AddDays(1),
                From = DateTime.Today,
            };

            if (isDefaultReport.HasValue)
            {
                srrRequest.IsDefaultReport = isDefaultReport.Value;
            }

            if (isChannelReport.HasValue)
            {
                srrRequest.IsChannelReport = isChannelReport.Value;
            }

            var searchResultReports = await _searchResultReportService.GetSearchResultReportsAsync(srrRequest, new CancellationToken());
            return searchResultReports?.Data?.Any(r => r.ContentTypeId == youTubeContentTypeId) == true || youTubeContentTypeId == 0;
        }

        public async Task<bool> HasTodayTwitterReport(bool? isDefaultReport = null, bool? isChannelReport = null)
        {
            var twitterContentTypeId = await GetTwitterContentTypeId();
            GetSearchResultReportRequest srrRequest = new GetSearchResultReportRequest
            {
                IsSuccess = true,
                To = DateTime.Today.AddDays(1),
                From = DateTime.Today,
            };

            if(isDefaultReport.HasValue)
            {
                srrRequest.IsDefaultReport = isDefaultReport.Value;
            }

            if (isChannelReport.HasValue)
            {
                srrRequest.IsChannelReport = isChannelReport.Value;
            }

            var searchResultReports = await _searchResultReportService.GetSearchResultReportsAsync(srrRequest, new CancellationToken());
            return searchResultReports?.Data?.Any(r => r.ContentTypeId == twitterContentTypeId) == true || twitterContentTypeId == 0;
        }

        public async Task AddSearchResultReport(SocialMediaDataResponse response, int contentTypeId, bool isDefaultReport = false, bool isChannelReport = false)
        {
            response = response ?? new SocialMediaDataResponse();
            string errors = response?.Errors?.FormatErrorMessages() ?? string.Empty;
            bool isSuccess = response.SuccessResults > 0;
            if (response.SuccessResults == 0 && response.DuplicateResults > 0)
            {
                isSuccess = true;
                response.ReportMessage = $"No new results saved. {response.DuplicateResults} found.";
            }
                
            try
            {
                var result = await _searchResultReportService.CreateSearchResultReportAsync(new SearchResultReportDto
                {
                    ContentTypeId = contentTypeId,
                    IsSuccess = isSuccess,
                    ReportDate = DateTime.Today,
                    IsDefaultReport = isDefaultReport,
                    IsChannelReport = isChannelReport,
                    ErrorMessage = response?.Errors?.FormatErrorMessages()

                }, new CancellationToken());

                if (!result.Success)
                {
                    response?.Errors.Add($"Error saving search result report. {contentTypeId}, IsDefault: {isDefaultReport}, IsChannel: {isChannelReport}. {result.Message}");
                }

                if(result.Success && isSuccess && response != null)
                {
                    if (System.String.IsNullOrEmpty(response.ReportMessage))
                    {
                        response.ReportMessage = $"Successfully saved {response.SuccessResults} search results";
                        if(response.DuplicateResults > 0)
                        {
                            response.ReportMessage += $". {response.DuplicateResults} duplicates found.";
                        }
                    }
                }
            }
            catch(Exception e)
            {
                response.Errors.Add($"Error saving search result report. {contentTypeId}, IsDefault: {isDefaultReport}, IsChannel: {isChannelReport}. {e.Message} ");
            }
        }
        #endregion SearchReportResultsRegion


        #region Reset

        public async Task ClearSearchResults()
        {
            try
            {
                var resetData = await _resetService.ClearAllCache();
                if (!resetData.Success)
                {
                    throw new Exception($"Error clearing search results. {resetData.Message}");
                }
            }
            catch (Exception e)
            {
                string message = e.Message;
                throw new Exception($"Error clearing search results. {message}");
            }
        }

        #endregion


        #region Channels
        public async Task<List<string>> GetChannelIdentifiersByContentTypeId(int id)
        {
            GetChannelsRequest chRequest = new GetChannelsRequest
            {
                IsActive = true,
                ContentTypeId = id
            };

            var channelData = await _channelService.GetChannelsAsync(chRequest, new CancellationToken());
            if (channelData?.Data == null || !channelData.Success || !channelData.Data.Any())
            {
                throw new Exception($"Error getting channels. {channelData?.Message}");
            }
            return channelData.Data.Select(c => c.Identifier ?? System.String.Empty).ToList();
        }

        public async Task<List<string>> GetYouTubeChannels()
        {
            var youTubeContentTypeId = await GetYouTubeContentTypeId();
            return await GetChannelIdentifiersByContentTypeId(youTubeContentTypeId);
        }

        public async Task<List<string>> GetTwitterChannels()
        {
            var twitterContentTypeId = await GetTwitterContentTypeId();
            return await GetChannelIdentifiersByContentTypeId(twitterContentTypeId);
        }
        #endregion


        #region DefaultTopics
        public async Task<List<string>> GetDefaultTopics(bool excludeTwitter = false)
        {
            var defaultTopics = new List<string>();
            var defaultTopicsData = await _defaultTopicService.GetDefaultTopicsAsync(new CancellationToken());

            if (defaultTopicsData?.Data == null || !defaultTopicsData.Data.Any())
                throw new Exception("NO DEFAULT TOPICS FOUND");

            if (!defaultTopicsData.Success && !System.String.IsNullOrEmpty(defaultTopicsData.Exception?.Message))
            {
                throw new Exception(defaultTopicsData?.Exception?.Message);
            }

            if (excludeTwitter)
            {
                defaultTopicsData.Data = defaultTopicsData.Data.Where(t => t.ExcludeFromTwitter != true).ToList();
            }

            return defaultTopicsData.Data.Select(t => t.Term).ToList();
        }
        #endregion

        #region Terms
        #endregion
        #region Quote
        public async Task<bool> CreateDailyQuote()
        {
            var quoteData = await _quoteService.CreateDailyQuote(new CancellationToken());
            if(quoteData?.Data == null)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}