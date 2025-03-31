using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace QueueOperator.CustomResourceDefinitions
{
    [KubernetesEntity(
        Group = "navitaire.com",
        ApiVersion = "v1",
        PluralName = "eventjobs",
        Kind = "EventJob"
    )]
    public class V1EventJobList 
        : IKubernetesObject<V1ListMeta>,
        IItems<V1EventJob>
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "v1";

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "JobQueueList";

        [JsonPropertyName("metadata")]
        public V1ListMeta Metadata { get; set; } = new();

        public IList<V1EventJob> Items { get; set; } = [];
    }
}
