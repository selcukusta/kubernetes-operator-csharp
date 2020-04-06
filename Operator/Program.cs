using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using Controller;
using Operator.Definitions;
using Operator.Extensions;

namespace Operator
{
    class Program
    {
        private static Kubernetes _client;
        private static KubernetesClientConfiguration _configuration;
        static async Task Main(string[] args)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IN_CLUSTER")))
            {
                _configuration = KubernetesClientConfiguration.InClusterConfig();
                Console.WriteLine($"== {Environment.GetEnvironmentVariable("OPERATOR_NAME")} will be started within cluster config... ==");
            }
            else
            {
                _configuration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                Console.WriteLine($"== Debug environment will be started from config file... ==");
            }

            _client = new Kubernetes(_configuration);
            var resource = new CustomResourceDefinition()
            {
                ApiVersion = "selcukusta.com/v1alpha1",
                PluralName = "linkedconfigmaps",
                Kind = "LinkedConfigMap",
                Namespace = Environment.GetEnvironmentVariable("WATCH_NAMESPACE") ?? "default"
            };

            var controller = new Controller<LinkedConfigMapCRD>(
                _client,
                resource,
                (WatchEventType type, LinkedConfigMapCRD item) =>
                {
                    Console.WriteLine($"== Event Type: {type}, Item Type: {item.Kind}, Item Name: {item.Metadata.Name} ==");
                    switch (type)
                    {
                        case WatchEventType.Added:
                            item.CreateConfigMapAsync(_client, resource.Namespace).ConfigureAwait(false);
                            foreach (var deployment in item.Spec.LinkedDeployments)
                            {
                                item.AddMountToDeploymentAsync(_client, resource.Namespace, deployment.Name, deployment.ContainerName, deployment.MountPath).ConfigureAwait(false);
                            }
                            break;

                        case WatchEventType.Modified:
                            {
                                item.UpdateConfigMapAsync(_client, resource.Namespace).ConfigureAwait(false);
                                foreach (var deployment in item.Spec.LinkedDeployments)
                                {
                                    item.UpdateDeploymentAsync(_client, resource.Namespace, deployment.Name).ConfigureAwait(false);
                                }
                                break;
                            }

                        case WatchEventType.Deleted:
                            item.DeleteConfigMapAsync(_client, resource.Namespace).ConfigureAwait(false);
                            foreach (var deployment in item.Spec.LinkedDeployments)
                            {
                                item.RemoveMountFromDeploymentAsync(_client, resource.Namespace, deployment.Name, deployment.ContainerName).ConfigureAwait(false);
                            }
                            break;
                    }
                });

            var cts = new CancellationTokenSource();
            await controller.StartAsync(cts).ConfigureAwait(false);
        }
    }
}
