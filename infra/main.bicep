targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of resource names')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = 'northeurope'

// Optional parameters
@description('Id of the principal to assign app permissions to')
param principalId string = ''

@description('GPT model deployment name')
param gpt4ModelDeploymentName string = 'gpt-4o'

// Generate unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName))

// Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: {
    'azd-env-name': environmentName
  }
}

// AI Hub and Project
module aiHub 'modules/ai-hub.bicep' = {
  name: 'ai-hub'
  scope: resourceGroup
  params: {
    name: 'aihub-${resourceToken}'
    location: location
    principalId: principalId
  }
}

// Azure OpenAI Service
module openAI 'modules/openai.bicep' = {
  name: 'openai'
  scope: resourceGroup
  params: {
    name: 'openai-${resourceToken}'
    location: location
    gpt4ModelDeploymentName: gpt4ModelDeploymentName
    principalId: principalId
  }
}

// Output the important values
output AZURE_LOCATION string = location
output AZURE_OPENAI_ENDPOINT string = openAI.outputs.endpoint
output AZURE_AI_PROJECT_ID string = aiHub.outputs.projectId
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output AZURE_OPENAI_SERVICE string = openAI.outputs.name
output AZURE_OPENAI_DEPLOYMENT string = gpt4ModelDeploymentName
