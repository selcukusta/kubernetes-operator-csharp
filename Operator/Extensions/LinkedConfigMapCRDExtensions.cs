using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Operator.Definitions;

namespace Operator.Extensions
{
    public static class LinkedConfigMapCRDExtensions
    {
        public static async Task<k8s.Models.V1Status> DeleteConfigMapAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace)
        {
            return await client.DeleteNamespacedConfigMapAsync(crd.Spec.Config.ConfigMapName, @namespace).ConfigureAwait(false);
        }
        public static async Task<k8s.Models.V1ConfigMap> UpdateConfigMapAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace)
        {
            var current = await client.ReadNamespacedConfigMapAsync(crd.Spec.Config.ConfigMapName, @namespace);
            current.Data = new Dictionary<string, string>() { { crd.Spec.Config.ConfigMapKey, crd.Spec.Config.ConfigMapValue } };
            return await client.ReplaceNamespacedConfigMapAsync(current, crd.Spec.Config.ConfigMapName, @namespace).ConfigureAwait(false);
        }
        public static async Task<k8s.Models.V1ConfigMap> CreateConfigMapAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace)
        {
            return await client.CreateNamespacedConfigMapAsync(new k8s.Models.V1ConfigMap
            {
                Metadata = new k8s.Models.V1ObjectMeta()
                {
                    Name = crd.Spec.Config.ConfigMapName,
                    OwnerReferences = new List<V1OwnerReference>(){
                        new V1OwnerReference{
                         ApiVersion = "v1",
                          BlockOwnerDeletion = true,
                          Controller = true,
                          Kind = crd.Kind,
                          Uid = crd.Metadata.Uid,
                          Name = crd.Metadata.Name
                    }}
                },
                Data = new Dictionary<string, string>() { { crd.Spec.Config.ConfigMapKey, crd.Spec.Config.ConfigMapValue } }
            }, @namespace).ConfigureAwait(false);
        }
        /***** DEPLOYMENTS ******/
        public static async Task<k8s.Models.V1Deployment> UpdateDeploymentAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace, string deploymentName)
        {
            var currentDeployment = await client.ReadNamespacedDeploymentAsync(deploymentName, @namespace).ConfigureAwait(false);
            return await currentDeployment.DoRestart(client);
        }
        public static async Task<k8s.Models.V1Deployment> RemoveMountFromDeploymentAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace, string deploymentName, string containerName)
        {
            var currentDeployment = await client.ReadNamespacedDeploymentAsync(deploymentName, @namespace).ConfigureAwait(false);

            if (currentDeployment == null)
            {
                return null;
            }

            if (currentDeployment.Spec.Template.Spec.Volumes == null || !currentDeployment.Spec.Template.Spec.Volumes.Any(x => x.ConfigMap.Name == crd.Spec.Config.ConfigMapName))
            {
                return null;
            }

            var currentVolume = currentDeployment.Spec.Template.Spec.Volumes.FirstOrDefault(x => x.ConfigMap.Name == crd.Spec.Config.ConfigMapName);
            currentDeployment.Spec.Template.Spec.Volumes.Remove(currentVolume);

            var container = currentDeployment.Spec.Template.Spec.Containers.FirstOrDefault(x => x.Name == containerName);
            if (container.VolumeMounts != null && container.VolumeMounts.Any(x => x.Name == crd.Spec.Config.ConfigMapName))
            {
                var currentMount = container.VolumeMounts.FirstOrDefault(x => x.Name == crd.Spec.Config.ConfigMapName);
                container.VolumeMounts.Remove(currentMount);
            }

            return await currentDeployment.DoRestart(client);
        }
        public static async Task<k8s.Models.V1Deployment> AddMountToDeploymentAsync(this LinkedConfigMapCRD crd, Kubernetes client, string @namespace, string deploymentName, string containerName, string mountPath)
        {
            var currentDeployment = await client.ReadNamespacedDeploymentAsync(deploymentName, @namespace).ConfigureAwait(false);

            if (currentDeployment == null)
            {
                return null;
            }

            if (currentDeployment.Spec.Template.Spec.Volumes == null)
            {
                currentDeployment.Spec.Template.Spec.Volumes = new List<V1Volume>();
            }

            if (!currentDeployment.Spec.Template.Spec.Volumes.Any(x => x.ConfigMap.Name == crd.Spec.Config.ConfigMapName))
            {
                currentDeployment.Spec.Template.Spec.Volumes.Add(new k8s.Models.V1Volume
                {
                    Name = crd.Spec.Config.ConfigMapName,
                    ConfigMap = new k8s.Models.V1ConfigMapVolumeSource { Name = crd.Spec.Config.ConfigMapName }
                });
            }

            var container = currentDeployment.Spec.Template.Spec.Containers.FirstOrDefault(x => x.Name == containerName);
            if (container.VolumeMounts == null)
            {
                container.VolumeMounts = new List<V1VolumeMount>();
            }

            if (!container.VolumeMounts.Any(x => x.Name == crd.Spec.Config.ConfigMapName))
            {
                container.VolumeMounts.Add(new V1VolumeMount { Name = crd.Spec.Config.ConfigMapName, MountPath = mountPath });
            }

            return await currentDeployment.DoRestart(client);
        }
        private static async Task<k8s.Models.V1Deployment> DoRestart(this k8s.Models.V1Deployment deployment, Kubernetes client)
        {
            if (deployment.Spec.Template.Metadata.Annotations == null)
            {
                deployment.Spec.Template.Metadata.Annotations = new Dictionary<string, string>();
            }

            var format = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";
            deployment.Spec.Template.Metadata.Annotations["kubernetes.io/restartedAt"] = DateTime.Now.ToString(format, DateTimeFormatInfo.InvariantInfo);
            deployment.Spec.Template.Metadata.Annotations["kubernetes.io/change-cause"] = "Mounted config is changed.";

            return await client.ReplaceNamespacedDeploymentAsync(deployment, deployment.Metadata.Name, deployment.Metadata.NamespaceProperty).ConfigureAwait(false);
        }
    }
}
