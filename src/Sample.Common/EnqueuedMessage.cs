using System;
//using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sample.Common
{
    public class EnqueuedMessage
    {
        [JsonPropertyName("day")]
        //[JsonProperty("day", NullValueHandling = NullValueHandling.Ignore)]        
        public string Day { get; set; }

        //[JsonProperty("eventName", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("eventName")]
        public string EventName { get; set; }

        //[JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        [JsonPropertyName("source")]
        public string Source { get; set; }

        public EnqueuedMessage()
        {
        }
    }
}
