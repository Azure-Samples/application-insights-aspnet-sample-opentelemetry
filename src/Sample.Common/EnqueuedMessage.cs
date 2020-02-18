using System.Text.Json.Serialization;

namespace Sample.Common
{
    public class EnqueuedMessage
    {
        [JsonPropertyName("day")]
        public string Day { get; set; }

        [JsonPropertyName("eventName")]
        public string EventName { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        public EnqueuedMessage()
        {
        }
    }
}
