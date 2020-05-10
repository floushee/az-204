#!/bin/bash
resourceGroup=floushee-az-204
storageAccount=flousheeaz204sa
functionApp=floushee-az-204-function-app
location=westeurope

az group create --name $resourceGroup \
    --location $location \
    --subscription="$subscriptionId"

az storage account create \
  --resource-group "$resourceGroup" \
  --name "$storageAccount" \
  --kind StorageV2 \
  --location centralus

az functionapp create \
  --resource-group "$resourceGroup" \
  --name "$functionApp" \
  --storage-account "$storageAccount" \
  --runtime node \
  --consumption-plan-location centralus
