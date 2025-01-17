using DopamineDetox.Domain.Dtos;
using DopamineDetox.ServiceAgent.Models.Responses;
using DopamineDetoxFunction.Extensions;
using DopamineDetoxFunction.Models;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Web;
using NTextCat;
using Microsoft.Extensions.Logging;

namespace DopamineDetoxFunction.Services
{
    public class YouTubeWrapperService : IYouTubeWrapperService
    {
        private readonly YouTubeService _youTubeService;
        private readonly IDopamineDetoxApiService _dopamineDetoxApiService;
        private readonly RankedLanguageIdentifier? _languageIdentifier;
        private readonly bool useEnglishOnly = true;

        public YouTubeWrapperService(YouTubeService youTubeService, IDopamineDetoxApiService dopamineDetoxApiService)
        {
            _youTubeService = youTubeService;
            _dopamineDetoxApiService = dopamineDetoxApiService;
            try
            {
                var profilePath = Path.Combine(Environment.CurrentDirectory, "Resources", "Core14.profile.xml");
                if (!File.Exists(profilePath))
                {
                    //_logger.Log(LogLevel.Warning, "Language profile not found. Disabling language detection.");
                    useEnglishOnly = false;
                }
                var factory = new RankedLanguageIdentifierFactory();
                _languageIdentifier = factory.Load(profilePath);
            }
            catch (Exception ex)
            {
                // Log the error and maybe fall back to a simpler detection method
                // or throw configuration exception
                //_logger.Log(LogLevel.Warning, ex, "Error loading language profile. Disabling language detection.");
                useEnglishOnly = false;
            }
        }

        public async Task ProcessYouTubeSearchTerms(IEnumerable<string> terms, SocialMediaDataResponse response, bool isHomePage = false, bool isChannel = false)
        {
            if(response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            foreach (var t in terms)
            {
                if (System.String.IsNullOrEmpty(t))
                {
                    continue;
                }

                try
                {
                    var term = t.ToLower().Trim();
                    var (ytResults, errors) = await GetYouTubeVideosEnhancedAsync(term);
                    if(errors != null || errors.Any())
                        response.Errors.AddRange(errors);

                    if (ytResults != null && ytResults.Any())
                    {
                        await SaveYTResultsAsync(ytResults, response, term, isHomePage: isHomePage, isChannel:isChannel);
                    }
                }
                catch (Exception e)
                {
                    response.Errors.Add($"Error adding search results for {t}. {e.Message}");
                    continue;
                }
            }
        }


        // wrap this on the consuming layer in a try catch to handle any errors with the YouTube/Google API
        public async Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)>GetYouTubeVideosAsync(string searchTerm)
        {
            var articles = new List<YouTubeResult>();
            var searchListRequest = _youTubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.Type = "video";
            searchListRequest.MaxResults = 10;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchListRequest.RelevanceLanguage = "en";

            SearchListResponse searchListResponse = null;
            List<string> errors = new List<string>();

            try
            {
                searchListResponse = await searchListRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw (new Exception($"Error searching for youtube videos: {ex.Message}"));
            }

            if(searchListResponse?.Items == null)
            {
                throw (new Exception($"Null value returned for searching youtube videos"));
            }

            foreach (var searchResult in searchListResponse.Items)
            {
                var videoId = searchResult?.Id?.VideoId ?? "N/A";
                var channelId = searchResult?.Snippet?.ChannelId ?? "N/A";

                try
                {
                    articles.Add(new YouTubeResult
                    {
                        Title = searchResult?.Snippet?.Title ?? "N/A",
                        Description = searchResult?.Snippet?.Description ?? "N/A",
                        VideoId = videoId,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        EmbedUrl = $"https://www.youtube.com/embed/{videoId}",
                        ChannelId = channelId,
                        ChannelTitle = searchResult?.Snippet?.ChannelTitle ?? "N/A",
                        ThumbnailUrl = searchResult?.Snippet?.Thumbnails?.Medium?.Url ?? "N/A",
                        PublishedAt = searchResult?.Snippet?.PublishedAtDateTimeOffset.GetValueOrDefault().DateTime ?? DateTime.Now
                    });
                }
                catch (Exception e)
                {
                    errors.Add($"Error processing search result: {e.Message}");

                }
            }

            return (articles, errors);
        }

        public async Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)> GetYouTubeVideosByHandleAsync(string handle)
        {
            // Step 1: Get the channel ID from the handle
            var channelsListRequest = _youTubeService.Channels.List("id");
            if(String.IsNullOrEmpty(handle))
            {
                throw (new Exception("Handle is null or empty"));
            }
            var atIndex = handle.IndexOf('@');
            if (atIndex == -1)
            {
                throw(new Exception("Handle does not contain an '@' symbol"));
            }
            var channelName= handle.Substring(atIndex);

            channelsListRequest.ForHandle = channelName;
            var channelsListResponse = await channelsListRequest.ExecuteAsync();

            if (channelsListResponse?.Items == null || channelsListResponse.Items.Count == 0)
            {
                throw (new Exception($"No channel found for handle '{handle}'"));
            }

            var channelId = channelsListResponse.Items[0].Id;

            // Step 2: Use the channel ID to search for videos
            var searchListRequest = _youTubeService.Search.List("snippet");

            searchListRequest.ChannelId = channelId;
            searchListRequest.Type = "video";
            searchListRequest.MaxResults = 10;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;

            var searchListResponse = await searchListRequest.ExecuteAsync();

            var articles = new List<YouTubeResult>();
            List<string> errors = new List<string>();

            foreach (var searchResult in searchListResponse.Items)
            {
                var videoId = searchResult?.Id?.VideoId ?? "N/A";

                try
                {
                    articles.Add(new YouTubeResult
                    {
                        Title = searchResult?.Snippet?.Title ?? "N/A",
                        Description = searchResult?.Snippet?.Description ?? "N/A",
                        VideoId = videoId,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        EmbedUrl = $"https://www.youtube.com/embed/{videoId}",
                        ChannelId = channelId,
                        ChannelTitle = searchResult?.Snippet?.ChannelTitle ?? "N/A",
                        ThumbnailUrl = searchResult?.Snippet?.Thumbnails?.Medium?.Url ?? "N/A",
                        PublishedAt = searchResult?.Snippet?.PublishedAtDateTimeOffset.GetValueOrDefault().DateTime ?? DateTime.Now
                    });
                }
                catch (Exception e)
                {
                    errors.Add($"Error processing search result: {e.Message}");
                }
            }

            return (articles,errors);
        }

        public async Task SaveYTResultsAsync(IEnumerable<YouTubeResult> ytResults, SocialMediaDataResponse response, string searchTerm, bool isHomePage = false, bool isChannel = false)
        {
            if(response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            try
            {
                int yt_ct_id = await _dopamineDetoxApiService.GetYouTubeContentTypeId();
                List<SearchResultDto> searchResults = ytResults.Select(yt => new SearchResultDto
                {
                    Title = HttpUtility.HtmlDecode(yt.Title),
                    Description = yt.Description,
                    Url = yt.Url,
                    UserName = yt.ChannelId,
                    VideoId = yt.VideoId,
                    EmbedUrl = yt.EmbedUrl,
                    ChannelName = yt.ChannelTitle,
                    ThumbnailUrl = yt.ThumbnailUrl,
                    PublishedAt = yt.PublishedAt,
                    Term = searchTerm,
                    ContentTypeId = yt_ct_id,
                    IsHomePage = isHomePage,
                    IsChannel = isChannel,
                    DateAdded = DateTime.Now
                }).ToList();

                var savedResults = await _dopamineDetoxApiService.AddMultipleSearchResultsAsync(searchResults);
                if(savedResults != null)
                {
                    response.SuccessResults += savedResults.SuccessCount;
                    response.DuplicateResults += savedResults.DuplicateCount;
                }
            }
            catch (Exception e)
            {
                throw (new Exception($"Error saving search results: {e.Message}"));
            }
        }

        public async Task<(IEnumerable<YouTubeResult> Results, List<string> Errors)> GetYouTubeVideosEnhancedAsync(string searchTerm)
        {
            var articles = new List<YouTubeResult>();
            var errors = new List<string>();

            // Calculate date range for fresh content
            var publishedAfter = DateTime.UtcNow.AddDays(-7); // Adjust window as needed

            // Initial search request setup
            var searchListRequest = _youTubeService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.Type = "video";
            searchListRequest.MaxResults = 25;
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date; // Changed back to date for fresh content
            searchListRequest.PublishedAfterDateTimeOffset = publishedAfter; // Only get videos from last 7 days
            searchListRequest.RelevanceLanguage = "en";
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.None;
            searchListRequest.VideoCaption = SearchResource.ListRequest.VideoCaptionEnum.ClosedCaption;
            searchListRequest.RegionCode = "US";
            searchListRequest.TopicId = "/m/09s1f";

            SearchListResponse searchListResponse = null;

            try
            {
                searchListResponse = await searchListRequest.ExecuteAsync();

                if (searchListResponse?.Items == null)
                {
                    throw new Exception("Null value returned for searching youtube videos");
                }

                var videoIds = searchListResponse.Items
                    .Select(x => x.Id.VideoId)
                    .ToList();

                if (!videoIds.Any())
                {
                    return (Enumerable.Empty<YouTubeResult>(), new List<string> { "No new videos found" });
                }

                var videoRequest = _youTubeService.Videos.List("snippet,contentDetails,statistics");
                videoRequest.Id = string.Join(",", videoIds);
                var videoResponse = await videoRequest.ExecuteAsync();
                var videoDetails = videoResponse.Items.ToDictionary(v => v.Id);

                foreach (var searchResult in searchListResponse.Items)
                {
                    var videoId = searchResult?.Id?.VideoId ?? "N/A";
                    var channelId = searchResult?.Snippet?.ChannelId ?? "N/A";

                    try
                    {
                        if (!videoDetails.TryGetValue(videoId, out var videoInfo))
                            continue;

                        //// Language detection for title and description
                        if (!IsEnglishContent(searchResult.Snippet.Title) ||
                            !IsEnglishContent(searchResult.Snippet.Description))
                            continue;

                        // Calculate combined relevance and freshness score
                        var relevanceScore = CalculateRelevanceScore(
                            searchTerm,
                            searchResult.Snippet.Title,
                            searchResult.Snippet.Description,
                            videoInfo,
                            searchResult.Snippet.PublishedAtDateTimeOffset.GetValueOrDefault()
                        );

                        // Skip if combined score is too low
                        if (relevanceScore < 0.4)
                            continue;

                        var result = new YouTubeResult
                        {
                            Title = searchResult?.Snippet?.Title ?? "N/A",
                            Description = searchResult?.Snippet?.Description ?? "N/A",
                            VideoId = videoId,
                            Url = $"https://www.youtube.com/watch?v={videoId}",
                            EmbedUrl = $"https://www.youtube.com/embed/{videoId}",
                            ChannelId = channelId,
                            ChannelTitle = searchResult?.Snippet?.ChannelTitle ?? "N/A",
                            ThumbnailUrl = searchResult?.Snippet?.Thumbnails?.Medium?.Url ?? "N/A",
                            PublishedAt = searchResult?.Snippet?.PublishedAtDateTimeOffset.GetValueOrDefault().DateTime ?? DateTime.Now,
                        };

                        articles.Add(result);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"Error processing search result: {e.Message}");
                    }
                }

                // Sort by combined score and take top 10
                return (articles.OrderByDescending(a => a.PublishedAt), errors);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching for youtube videos: {ex.Message}");
            }
        }

        private double CalculateRelevanceScore(string searchTerm, string title, string description, Video videoInfo, DateTimeOffset publishedAt)
        {
            var score = 0.0;

            // Title match weight (0.3)
            if (title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 0.3;

            // Description match weight (0.15)
            if (description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                score += 0.15;

            // Engagement metrics weight (0.25)
            var viewCount = videoInfo.Statistics?.ViewCount ?? 0;
            var likeCount = videoInfo.Statistics?.LikeCount ?? 0;

            if (viewCount > 0 && likeCount > 0)
            {
                var engagementRate = (double)likeCount / viewCount;
                score += Math.Min(0.25, engagementRate * 100);
            }

            // Freshness weight (0.3)
            var hoursOld = (DateTime.UtcNow - publishedAt).TotalHours;
            var freshnessScore = Math.Max(0, 0.3 * (168 - hoursOld) / 168); // 168 hours = 7 days
            score += freshnessScore;

            return score;
        }

        private bool IsEnglishContent(string text)
        {
            if (!useEnglishOnly || _languageIdentifier == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                var languages = _languageIdentifier.Identify(text);
                var mostLikelyLanguage = languages.FirstOrDefault();

                // Check if English is the most likely language with reasonable confidence
                return mostLikelyLanguage?.Item1?.Iso639_2T == "eng";
            }
            catch
            {
                // If detection fails, default to true to avoid false negatives
                return true;
            }
        }
    }
}