#!/bin/bash

rg=floushee-az-204
name=flousheearmsa

# deploy template
az group deployment create --resource-group $rg --template-file ./template.json --parameters ./parameters.json

# delete resources
az storage account delete --resource-group $rg --name $name
