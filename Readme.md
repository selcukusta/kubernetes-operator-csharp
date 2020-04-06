# Developing Kubernetes Operator with C#

## What's the purpose?

It can be called as "replica of my [another repository](https://github.com/selcukusta/kubernetes-operator)" which was written in Go.

I just wanted to be sure about we can develop a Kubernetes Operator with C#.

I must be honest, it's an experimental project and it's not suited for the production environment. If you'll use it on the production and fix or change something, please let me know.

## Create the demo environment

```bash
# Only create a minikube :)
minikube start
```

## Debug the operator

```bash
# Run operator console application
dotnet run --project Operator
# You should see this message;
# == Debug environment will be started from config file... ==

# Create prerequested definitions
kubectl apply -f .deploy/prerequests

# Create nginx deployment
kubectl apply -f .deploy/nginx.yaml

# Sure config file is not mounted automatically
kubectl exec -it $(kubectl get pod -l app=nginx -o jsonpath="{.items[0].metadata.name}") cat /tmp/basic.conf
# Output should be;
# cat: can't open '/tmp/basic.conf': No such file or directory
# command terminated with exit code 1

# Then apply the LinkedConfigMap custom resource
kubectl apply -f .deploy/6_cr.yaml

# Sure config file is mounted correctly
kubectl exec -it $(kubectl get pod -l app=nginx -o jsonpath="{.items[0].metadata.name}") cat /tmp/basic.conf
# Output should be;
# ;hello=neptune
```

## Deploy the operator

```bash
# Build the operator image
docker image build -t selcukusta/cs-operator:1.0.0 .

# Push it
docker image push selcukusta/cs-operator:1.0.0

# Change build image for operator deployment
sed -i "" 's|REPLACE_IMAGE|selcukusta/cs-operator:1.0.0|g' .deploy/5_operator.yaml

# Create prerequested definitions
kubectl apply -f .deploy/prerequests

# Create nginx deployment
kubectl apply -f .deploy/nginx.yaml

# Deploy the app-operator
kubectl apply -f .deploy/5_operator.yaml

# Then apply the LinkedConfigMap custom resource
kubectl apply -f .deploy/6_cr.yaml
```

```bash
# You can check the result on another terminal tab
while true; do sleep 1; kubectl exec -it $(kubectl get pod -l app=nginx -o jsonpath="{.items[0].metadata.name}") cat /tmp/basic.conf;done`

# You can check the operator logs also
kubectl logs $(kubectl get pod -l name=cs-operator -o jsonpath="{.items[0].metadata.name}")
### Output will be like that after your custom resource actions
# == cs-operator will be started within cluster config... ==
# == Event Type: Added, Item Type: LinkedConfigMap, Item Name: dummy-linked-configmap ==
# == Event Type: Modified, Item Type: LinkedConfigMap, Item Name: dummy-linked-configmap ==
# == Event Type: Deleted, Item Type: LinkedConfigMap, Item Name: dummy-linked-configmap ==
```

## References

> https://radu-matei.com/blog/kubernetes-controller-csharp/

> https://github.com/engineerd/kubecontroller-csharp

> https://github.com/kubernetes-client/csharp
