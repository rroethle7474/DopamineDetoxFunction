using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DopamineDetoxFunction.Models
{
    public class TwitterApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        [JsonProperty(PropertyName = "Errors")]
        private object? _errors;

        [JsonIgnore]
        public IEnumerable<string> Errors
        {
            get
            {
                if (_errors is JArray jArray)
                {
                    return jArray.Select(token => token.ToString());
                }
                else if (_errors is string[] stringArray)
                {
                    return stringArray;
                }
                else if (_errors is string singleError)
                {
                    return new[] { singleError };
                }
                return Enumerable.Empty<string>();
            }
        }
    }
}
