param location string
param keyVaultName string

resource az_oai 'Microsoft.CognitiveServices/accounts@2024-06-01-preview' = {
  name: 'ai-${uniqueString(subscription().subscriptionId)}'
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  tags: {}
  properties: {
    customSubDomainName: 'ai-${uniqueString(subscription().subscriptionId)}'
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

resource oai_deployment 'Microsoft.CognitiveServices/accounts/deployments@2024-06-01-preview' = {
  name: 'gpt-4o'
  parent: az_oai
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
    currentCapacity: 1
    versionUpgradeOption: 'OnceCurrentVersionExpired'
    //RAI == Responsible AI
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      name:  'standard'
      family:  'A'
    }
    accessPolicies: []
    enableRbacAuthorization: true
    enabledForTemplateDeployment: true
    tenantId: tenant().tenantId
  }
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview' = {
  name: 'accountKey'
  parent: vault
  properties: {
      value: az_oai.listKeys().key1
  }
}

output accountName string = az_oai.properties.customSubDomainName
output endpoint string    = az_oai.properties.endpoint
