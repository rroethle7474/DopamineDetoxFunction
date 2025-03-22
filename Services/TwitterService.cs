using DopamineDetox.Domain.Dtos;
using DopamineDetox.ServiceAgent.Models.Responses;
using DopamineDetoxFunction.Extensions;
using DopamineDetoxFunction.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace DopamineDetoxFunction.Services
{
    public class TwitterService : ITwitterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _xLoginUrl;
        private readonly IDopamineDetoxApiService _dopamineDetoxApiService;
        private readonly ITwitterEmbedService _twitterEmbedService;

        public TwitterService(HttpClient httpClient, IConfiguration configuration, IDopamineDetoxApiService dopamineDetoxApiService, ITwitterEmbedService twitterEmbedService)
        {
            _httpClient = httpClient;
            _xLoginUrl = configuration["XLoginUrl"] ?? "https://x.com/i/flow/login";
            _dopamineDetoxApiService = dopamineDetoxApiService;
            _twitterEmbedService = twitterEmbedService;
        }

        public async Task<bool> ClearSearchResults()
        {
            var response = await _httpClient.PostAsync("ResetSearchCache", null);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }

        public async Task<Dictionary<string, IEnumerable<TwitterResult>>> GetTwitterResultsAsync(IEnumerable<string> terms, 
            SocialMediaDataResponse socialMediaDataResponse, bool isDefaultSearch = false, bool isChannelSearch = false)
        {
            if(socialMediaDataResponse == null)
            {
                throw new ArgumentNullException(nameof(socialMediaDataResponse));
            }

            var requestBody = new
            {
                url = _xLoginUrl,
                search_queries = terms,
                isDefault = isDefaultSearch
            };

            string searchUrl = "SearchResults";

            if (isChannelSearch)
            {
                searchUrl = "ChannelResults";
            }

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(searchUrl, httpContent);

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonConvert.DeserializeObject<TwitterApiResponse<Dictionary<string, JArray>>>(content);

            var results = new Dictionary<string, IEnumerable<TwitterResult>>();

            if (!response.IsSuccessStatusCode || apiResponse == null || !apiResponse.Success)
            {
                socialMediaDataResponse.Errors.Add($"Error getting Twitter results: {apiResponse?.Message}");
                return results;
            }

            socialMediaDataResponse.ReportMessage = apiResponse.Message;

            if (apiResponse.Data != null)
            {
                foreach (var kvp in apiResponse.Data)
                {
                    try
                    {
                        var key = kvp.Key;
                        var value = kvp.Value.ToString();

                        var twitterResults = JsonConvert.DeserializeObject<List<TwitterResult>>(value);
                        results[key] = twitterResults ?? new List<TwitterResult>();
                    }
                    catch (Exception ex)
                    {
                        socialMediaDataResponse.Errors.Add($"Error processing property '{kvp.Key}': {ex.Message}");
                    }
                }
            }

            if (apiResponse.Errors != null && apiResponse.Errors.Any())
            {
                foreach (var error in apiResponse.Errors)
                {
                    socialMediaDataResponse.Errors.Add(error);
                }
            }

            return results;
        }

        public async Task SaveTwitterResultsAsync(Dictionary<string, IEnumerable<TwitterResult>> twitterArticles, SocialMediaDataResponse socialMediaDataResponse, bool isHomePage=false, 
            bool isChannelResult = false)
        {
            if(socialMediaDataResponse == null)
            {
                throw new ArgumentNullException(nameof(socialMediaDataResponse));
            }

            int twitter_ct_id = await _dopamineDetoxApiService.GetTwitterContentTypeId();

            foreach (var kvp in twitterArticles)
            {
                try
                {
                    string searchTerm = kvp.Key;
                    List<SearchResultDto> searchResults = new List<SearchResultDto>();
                    IEnumerable<TwitterResult> results = kvp.Value;

                    foreach (var t in results)
                    {
                        try
                        {
                            var embedHtml = await _twitterEmbedService.GetHtmlEmbeddingAsync(t.EmbedUrl);
                            searchResults.Add(new SearchResultDto
                            {
                                Title = t.Channel + " : " + searchTerm,
                                Description = t.Description,
                                Url = t.EmbedUrl,
                                UserName = t.Username,
                                EmbedUrl = t.EmbedUrl,
                                ChannelName = t.Channel,
                                PublishedAt = t.PublishedAt,
                                Term = searchTerm,
                                ContentTypeId = twitter_ct_id,
                                IsHomePage = isHomePage,
                                DateAdded = DateTime.Now,
                                EmbeddedHtml = embedHtml,
                                TopSearchResult = false,
                                IsChannel = isChannelResult
                            });
                        }
                        catch(Exception e)
                        {
                            socialMediaDataResponse.Errors.Add($"Embed Html error for '{t.EmbedUrl}': {e.Message}. Inner exception: {e.InnerException?.Message ?? "none"}");
                            continue;
                        }
                    }

                    var savedResults = await _dopamineDetoxApiService.AddMultipleSearchResultsAsync(searchResults);
                    socialMediaDataResponse.SuccessResults += savedResults.SuccessCount;
                    socialMediaDataResponse.DuplicateResults += savedResults.DuplicateCount;
                }
                catch (Exception e)
                {
                    socialMediaDataResponse.Errors.Add($"Error saving search results for '{kvp.Key}': {e.Message}");

                }
            }

        }
    }
}