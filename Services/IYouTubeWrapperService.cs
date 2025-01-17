using DopamineDetoxFunction.Models;

namespace DopamineDetoxFunction.Services
{
    public interface IYouTubeWrapperService
    {
        Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)> GetYouTubeVideosAsync(string searchTerm);
        // uses language detection and relevance to return results
        Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)> GetYouTubeVideosEnhancedAsync(string searchTerm);
        Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)> GetYouTubeVideosByHandleAsync(string handle);
        Task SaveYTResultsAsync(IEnumerable<YouTubeResult> ytResults, SocialMediaDataResponse response, string searchTerm, bool isHomePage = false, bool isChannel = false);
        Task ProcessYouTubeSearchTerms(IEnumerable<string> terms, SocialMediaDataResponse response, bool isHomePage = false, bool isChannel = false);
    }
}
