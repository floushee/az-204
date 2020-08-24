#!/bin/bash
rg=floushee-az-204
vmname=floushee-ubuntu-01
user=floushee

# create VM
az vm create \
  --resource-group $rg \
  --name $vmname \
  --image UbuntuLTS \
  --admin-username $user \
  --generate-ssh-keys

# configure ports
az vm open-port --port 80 --resource-group $rg --name $vmname

# ssh to VM
# publicIpAddress= <- output of the vm create command
# ssh $floushee@publicIpAddress
