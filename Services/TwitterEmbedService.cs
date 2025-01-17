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
                string baseUrl = _httpClient?.BaseAddress?.ToString();
                string encodedUrl = System.Net.WebUtility.UrlEncode(url);
                string requestUrl = $"{baseUrl}{encodedUrl}?format=json&omit_script=true&lang=en";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var jObject = JObject.Parse(jsonResponse);

                // Extract the 'html' property and decode it
                var html = jObject["html"]?.ToString();
                html = System.Net.WebUtility.HtmlDecode(html);
                if(String.IsNullOrEmpty(html))
                {
                    throw new Exception("No HTML found in the Twitter embed response");
                }
                return html;
            }
            catch(Exception ex)
            {
                throw new Exception($"Error getting Twitter embed: {ex.Message}");
            }
        }
    }
}