using System.Text.Json.Serialization;

namespace QueueOperator.CustomResourceDefinitions
{
    public class V1JobQueueStatus
    {
        [JsonPropertyName("queue")]
        public Queue<string> Queue { get; set; } = [];

        [JsonPropertyName("activeJob")]
        public string? ActiveJob { get; set; }
    }
}
