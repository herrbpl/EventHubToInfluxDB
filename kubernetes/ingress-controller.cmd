rem from https://kubernetes.github.io/ingress-nginx/deploy/
REM Ingress controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/master/deploy/static/mandatory.yaml
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/master/deploy/static/provider/cloud-generic.yaml

rem from https://docs.cert-manager.io
rem cert-manager
rem # Create a namespace to run cert-manager in
kubectl create namespace cert-manager
rem # Disable resource validation on the cert-manager namespace
kubectl label namespace cert-manager certmanager.k8s.io/disable-validation=true
rem # Install the CustomResourceDefinitions and cert-manager itself
kubectl apply -f https://github.com/jetstack/cert-manager/releases/download/v0.8.1/cert-manager.yaml


rem # Create ClusterIssuers
kubectl apply -f letsencrypt-staging.yaml
kubectl apply -f letsencrypt-prod.yaml


rem # recreate cert-manager with issuer-shim
kubectl apply -f cert-manager-with-shim.yaml

rem # register dns mnt.gttarkvara.ee for ingress controller public ip

rem # apply ingress controller
kubectl apply -f ingress.yaml