using k8s;
using k8s.Models;

namespace Controller
{
    public abstract class CustomResource : KubernetesObject
    {
        public V1ObjectMeta Metadata { get; set; }
    }

    public abstract class CustomResource<TSpec> : CustomResource
    {
        public TSpec Spec { get; set; }
    }

    public class CustomResourceDefinition
    {
        public string ApiVersion { get; set; }

        public string PluralName { get; set; }

        public string Kind { get; set; }

        public string Namespace { get; set; }
    }
}