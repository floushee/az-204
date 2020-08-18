# Example Code Only
# Provided as-is

param([string]$subscriptionName = "Your Sponsorship", [string]$deploymentType = "Prod", [string]$location = "eastus2", [string]$resourceGroupName="WBC_Production", [string]$localPath="C:\Deployments" )

Login-AzureRmAccount 

$subResult = Select-AzureRmSubscription -SubscriptionName "$subscriptionName"  

# List all the Logic Apps to be deployed in order needed
$LogicAppList = @("PollWebsites")

Foreach($logicApp in $LogicAppList)
{
    $results = New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile "$localPath\$logicApp\WBC.$logicApp.json" -TemplateParameterFile "$localPath\$logicApp\WBC.$logicApp.parameters.$deploymentType.json" 
    Write-Output $results
}
