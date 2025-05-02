@description('The name of the Azure OpenAI service')
param name string

@description('The Azure region for the resources')
param location string

@description('GPT model deployment name')
param gpt4ModelDeploymentName string

@description('The ID of the principal to assign permissions to')
param principalId string = ''

// Create Azure OpenAI service
resource openAI 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Deploy the GPT-4o model
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAI
  name: gpt4ModelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 1
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Assign permissions if principalId is provided
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(openAI.id, principalId, 'cogservicesuser')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services User
    principalId: principalId
    principalType: 'User'
  }
}

// Outputs
output id string = openAI.id
output name string = openAI.name
output endpoint string = 'https://${openAI.properties.customSubDomainName}.openai.azure.com/'
output deploymentName string = gpt4oDeployment.name
