using System.Text.Json.Serialization;

using k8s.Models;

namespace QueueOperator.CustomResourceDefinitions
{
    public class V1EventJobSpec
    {
        [JsonPropertyName("eventName")]
        public string? EventName { get; set; }

        [JsonPropertyName("jobTemplate")]
        public V1JobTemplateSpec? JobTemplate { get; set; }
    }
}
