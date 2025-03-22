using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace DopamineDetoxFunction.Services
{
    public class TwitterEmbedService : ITwitterEmbedService
    {
        private readonly HttpClient _httpClient;

        public TwitterEmbedService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetHtmlEmbeddingAsync(string url)
        {
            if(String.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            
            try
            {
                string encodedUrl = System.Net.WebUtility.UrlEncode(url);
                // Properly format the URL with the 'url' parameter followed by other parameters
                string requestUrl = $"{_httpClient.BaseAddress}?url={encodedUrl}&format=json&omit_script=true&lang=en";
                
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var response = await _httpClient.SendAsync(request);
                
                // Log the status code and response content for debugging
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP error {(int)response.StatusCode} ({response.StatusCode}). Response: {errorContent}");
                }
                
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    var jObject = JObject.Parse(jsonResponse);

                    // Extract the 'html' property and decode it
                    var html = jObject["html"]?.ToString();
                    html = System.Net.WebUtility.HtmlDecode(html);
                    if(String.IsNullOrEmpty(html))
                    {
                        throw new Exception($"No HTML found in the Twitter embed response. Full response: {jsonResponse}");
                    }
                    return html;
                }
                catch (Exception jsonEx)
                {
                    throw new Exception($"Error parsing JSON response: {jsonEx.Message}. Raw response: {jsonResponse}", jsonEx);
                }
            }
            catch(Exception ex)
            {
                throw new Exception($"Error getting Twitter embed for URL '{url}': {ex.Message}", ex);
            }
        }
    }
}