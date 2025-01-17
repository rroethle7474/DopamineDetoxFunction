using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopamineDetoxFunction.Models
{
    public class SocialMediaTimerDataResponse
    {
        public string? Message { get; set; }
        public DateTime ExecutionTime { get; set; }
        public DateTime? NextExecutionTime { get; set; }
        public SocialMediaDataResponse DefaultYouTubeResponse { get; set; } = new SocialMediaDataResponse();
        public SocialMediaDataResponse DefaultXResponse { get; set; } = new SocialMediaDataResponse();
        public SocialMediaDataResponse YouTubeResponse { get; set; } = new SocialMediaDataResponse();
        public SocialMediaDataResponse XResponse { get; set; } = new SocialMediaDataResponse();
        public SocialMediaDataResponse YouTubeChannelResponse { get; set; } = new SocialMediaDataResponse();
        public SocialMediaDataResponse XChannelResponse { get; set; } = new SocialMediaDataResponse();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
