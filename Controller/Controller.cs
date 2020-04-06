using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;

namespace Controller
{
    public class Controller<T> where T : CustomResource
    {
        public delegate void Handle(WatchEventType ev, T item);
        private readonly Kubernetes _client;
        private readonly CustomResourceDefinition _crd;
        private readonly Handle _handle;

        public Controller(Kubernetes client, CustomResourceDefinition crd, Handle handle)
        {
            _client = client;
            _crd = crd;
            _handle = handle;
        }

        public async Task StartAsync(CancellationTokenSource tokenSource)
        {
            var result = await _client.ListNamespacedCustomObjectWithHttpMessagesAsync(
                group: _crd.ApiVersion.Split('/')[0],
                version: _crd.ApiVersion.Split('/')[1],
                namespaceParameter: _crd.Namespace,
                plural: _crd.PluralName,
                watch: true)
                .ConfigureAwait(false);

            var token = tokenSource.Token;
            using (result.Watch<T, object>((type, item) => _handle(type, item)))
            {
                var resetEvent = new ManualResetEventSlim(false);
                if (!tokenSource.IsCancellationRequested)
                {
                    using (CancellationTokenRegistration ctr = token.Register(() => resetEvent.Set()))
                    {
                        resetEvent.Wait();
                    }
                }
            }
        }
    }
}