using DopamineDetoxFunction.Models;

namespace DopamineDetoxFunction.Services
{
    public interface ITwitterService
    {
        Task<Dictionary<string, IEnumerable<TwitterResult>>> GetTwitterResultsAsync(IEnumerable<string> searchTerms, SocialMediaDataResponse response, bool isDefaultSearch = false, bool isChannelSearch = false);
        Task SaveTwitterResultsAsync(Dictionary<string, IEnumerable<TwitterResult>> twitterArticles, SocialMediaDataResponse response, bool isHomePage = false, bool isChannelResult = false);
        Task<bool> ClearSearchResults();
    }
}
