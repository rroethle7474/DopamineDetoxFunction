using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopamineDetoxFunction.Models
{
    public class SocialMediaDataResponse
    {
        public string? ReportMessage { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int DuplicateResults { get; set; }
        public int SuccessResults { get; set; }
    }
}
