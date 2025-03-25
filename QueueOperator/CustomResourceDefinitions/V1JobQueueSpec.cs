using System.Text.Json.Serialization;

namespace QueueOperator.CustomResourceDefinitions
{
    public class V1JobQueueSpec
    {
        [JsonPropertyName("selector")]
        public Dictionary<string, string> Selector { get; set; } = [];
    }
}
