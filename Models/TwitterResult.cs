using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace DopamineDetoxFunction.Models
{
    [Table("TwitterResults")]
public class TwitterResult
    {
        public int Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("published_date")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("embed_url")]
        public string EmbedUrl { get; set; } = "";

        [JsonProperty("channel")]
        public string Channel { get; set; } = "";
        [JsonProperty("username")]
        public string Username { get; set; } = "";
    }
}
