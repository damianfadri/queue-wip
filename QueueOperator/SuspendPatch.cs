using System.Text.Json.Serialization;

namespace QueueOperator
{
    public class SuspendPatch
    {
        [JsonPropertyName("spec")]
        public SuspendPatchSpec? Spec { get; set; }

        public SuspendPatch(bool suspend)
        {
            Spec = new SuspendPatchSpec { Suspend = suspend };
        }
    }

    public class SuspendPatchSpec
    {
        [JsonPropertyName("suspend")]
        public bool Suspend { get; set; }
    }
}
