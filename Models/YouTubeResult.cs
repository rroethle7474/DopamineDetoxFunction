using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopamineDetoxFunction.Models
{
    [Table("YouTubeResults")]
    public class YouTubeResult
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public string VideoId { get; set; } = "";
        public string EmbedUrl { get; set; } = "";
        public string ChannelId { get; set; } = "";
        public string ChannelTitle { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
    }
}
