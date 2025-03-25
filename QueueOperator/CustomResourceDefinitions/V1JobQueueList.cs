using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace QueueOperator.CustomResourceDefinitions
{
    [KubernetesEntity(
        Group = "navitaire.com",
        ApiVersion = "v1",
        PluralName = "jobqueues",
        Kind = "JobQueue"
    )]
    public class V1JobQueueList 
        : IKubernetesObject<V1ListMeta>,
        IItems<V1JobQueue>
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "v1";

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "JobQueueList";

        [JsonPropertyName("metadata")]
        public V1ListMeta Metadata { get; set; } = new();
        public IList<V1JobQueue> Items { get; set; } = [];
    }
}
