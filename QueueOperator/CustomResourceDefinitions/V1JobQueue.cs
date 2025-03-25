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
    public class V1JobQueue 
        : IKubernetesObject<V1ObjectMeta>,
        ISpec<V1JobQueueSpec>,
        IStatus<V1JobQueueStatus>
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "navitaire.com/v1";

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "JobQueue";

        [JsonPropertyName("metadata")]
        public V1ObjectMeta Metadata { get; set; } = new();

        [JsonPropertyName("spec")]
        public V1JobQueueSpec Spec { get; set; } = new();

        [JsonPropertyName("status")]
        public V1JobQueueStatus Status { get; set; } = new();
    }
}
