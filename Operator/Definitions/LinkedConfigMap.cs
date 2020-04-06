using System.Collections.Generic;

namespace Operator.Definitions
{
    public class LinkedConfigMap
    {
        public IList<DeploymentSpecification> LinkedDeployments { get; set; }
        public ConfigSpecification Config { get; set; }
    }
}
