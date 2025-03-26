using DopamineDetox.Domain.Dtos;
using DopamineDetoxFunction.Models;

namespace DopamineDetoxFunction.Services
{
    public interface IDopamineDetoxApiService
    {
        #region Channels
        Task<List<string>> GetChannelIdentifiersByContentTypeId(int id);
        Task<List<string>> GetYouTubeChannels();
        Task<List<string>> GetTwitterChannels();
        #endregion

        #region ContentTypes
        Task<IEnumerable<ContentTypeDto>> GetContentTypes();
        Task<int> GetYouTubeContentTypeId();
        Task<int> GetTwitterContentTypeId();
        #endregion

        #region DefaultTopics
        Task<List<string>> GetDefaultTopics(bool excludeTwitter = false);
        #endregion

        #region SearchResults
        Task<SearchResultsResponseDto> AddMultipleSearchResultsAsync(IEnumerable<SearchResultDto> searchResults);
        Task<IEnumerable<string>> GetSearchTerms(bool excludeTwitter = false);
        Task<IEnumerable<string>> GetMVPUserListAsync(SocialMediaDataResponse response);
        Task ClearMVPWeeklySearchResults(SocialMediaDataResponse response);
        #endregion


        #region SearchResultsReport
        Task<bool> HasTodayYouTubeReport(bool? isDefaultReport = null, bool? isChannelReport = null);
        Task<bool> HasTodayTwitterReport(bool? isDefaultReport = null, bool? isChannelReport = null);
        Task AddSearchResultReport(SocialMediaDataResponse response, int contentTypeId, bool isDefaultReport = false, bool isChannelReport = false);
        #endregion SearchResultsReport

        #region WeeklySearchResultReport
        Task<bool> HasWeeklySearchResultReport(SocialMediaDataResponse response);
        Task<bool> AddWeeklySearchResultReport(SocialMediaDataResponse response);
        Task<bool> EmailWeeklySearchReportByUser(string userId, SocialMediaDataResponse response);
        #endregion

        #region Reset
        Task ClearSearchResults();
        #endregion

        #region Quote
        Task<bool> CreateDailyQuote();
        #endregion
    }
}
