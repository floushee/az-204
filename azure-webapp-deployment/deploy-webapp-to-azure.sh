#!/bin/bash

repository=..
branch=..

az group create --location westeurope --name azure-webapp-rg

az appservice plan create \
    --resource-group azure-webbapp-rg \
    --name azure-webapp-sp \
    --is-linux \
    --number-of-workers 1 \
    --sku S1

az webapp create \
    --resource-group azure-webapp-rg \
    --plan azure-webapp-sp \
    --name azure-webapp

az webapp deployment source config --name azure-webapp \
    --resource-group azure-webapp-rg \
    --repo-url $repository \
    --branch $branch \
    --manual-integration
