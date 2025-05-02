@description('The name of the Azure AI Hub')
param name string

@description('The Azure region for the resources')
param location string

@description('The ID of the principal to assign permissions to')
param principalId string = ''

// Create an AI Hub (AI Studio)
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2025-01-01-preview' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: 'AI Hub for ${name}'
    description: 'Azure AI Hub workspace for Semantic Kernel demos'
    storageAccount: storageAccount.id
    keyVault: keyVault.id
    applicationInsights: appInsights.id
    hbiWorkspace: false
    allowPublicAccessWhenBehindVnet: true
    publicNetworkAccess: 'Enabled'
  }
}

// Create a default AI Project
resource aiProject 'Microsoft.MachineLearningServices/workspaces/projects@2023-06-01-preview' = {
  name: 'semantic-kernel-demo'
  parent: aiHub
  properties: {
    friendlyName: 'Semantic Kernel Demo Project'
    description: 'Project for Semantic Kernel demonstrations'
  }
}

// Create a Storage Account for the AI Hub
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: replace('st${name}', '-', '')
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// Create a Key Vault for the AI Hub
resource keyVault 'Microsoft.KeyVault/vaults@2024-12-01-preview' = {
  name: 'kv-${name}'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Create Application Insights for the AI Hub
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-${name}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// Assign permissions if principalId is provided
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(aiHub.id, principalId, 'contributor')
  scope: aiHub
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c') // Contributor role
    principalId: principalId
    principalType: 'User'
  }
}

// Outputs
output id string = aiHub.id
output name string = aiHub.name
output projectId string = aiProject.id
