#!/bin/bash
rg=floushee-az-204
name=flousheecosmosdb
collectionName=tickets
consistencyLevel=BoundedStaleness
maxIntervall=30


# --max-interval 
#     When used with Bounded Staleness consistency, this value represents the time amount of staleness (in seconds) tolerated.
# --enable-automatic-failover
#     Enables automatic failover of the write region in the rare event that the region is unavailable due to an outage
az cosmosdb create \
  --resource-group $rg \
  --name $name \
  --enable-automatic-failover true \
  --max-interval $maxIntervall \
  --default-consistency-level $consistencyLevel \
  --locations regionName=eastus failoverPriority=0 isZoneRedundant=False
  --locations regionName=westus failoverPriority=1 isZoneRedundant=False
