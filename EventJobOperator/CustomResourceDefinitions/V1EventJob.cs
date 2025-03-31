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
    public class V1EventJob 
        : IKubernetesObject<V1ObjectMeta>,
        ISpec<V1EventJobSpec>
    {
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = "navitaire.com/v1";

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "EventJob";

        [JsonPropertyName("metadata")]
        public V1ObjectMeta Metadata { get; set; } = new();

        [JsonPropertyName("spec")]
        public V1EventJobSpec Spec { get; set; } = new();
    }
}
