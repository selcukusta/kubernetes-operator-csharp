namespace Operator.Definitions
{
    public class DeploymentSpecification
    {
        public string Name { get; set; }
        public string ContainerName { get; set; }
        public string MountPath { get; set; }
    }
}
