param sku string = 'B2' 
param locationwest string = 'eastus' 
param locationnorth string = 'northeurope' 
var appServicePlanNameWest = 'fusion-cache-api-west-app-plan'
var appServicePlanNameNorth = 'fusion-cache-api-north-app-plan'
var webSiteNameWest1 = 'fusion-cache-api-west-1'
var webSiteNameWest2 = 'fusion-cache-api-west-2'
var webSiteNameNorth1 = 'fusion-cache-api-north-1'
var webSiteNameNorth2 = 'fusion-cache-api-north-2'
var logAnalyticsWorkspaceName  = 'fusion-cache-logAnalyticsWorkspace'
var applicationInsightsName  = 'fusion-cache-applicationInsights'

// redis 


// west
resource appServicePlanWest 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanNameWest
  location: locationwest
  sku: {
    name: sku
  }
  kind: 'app'
}

resource appServicew1 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteNameWest1
  location: locationwest
  properties: {
    serverFarmId: appServicePlanWest.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: '8.0'
	    metadata: [{
                name : 'CURRENT_STACK'
                value : 'dotnetcore'
            }]
    }
  }
}

resource appServicew2 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteNameWest2
  location: locationwest
  properties: {
    serverFarmId: appServicePlanWest.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: '8.0'
	    metadata: [{
                name : 'CURRENT_STACK'
                value : 'dotnetcore'
            }]
    }
  }
}

// nord
resource appServicePlanNorth 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanNameNorth
  location: locationnorth
  sku: {
    name: sku
  }
  kind: 'app'
}

resource appServicen1 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteNameNorth1
  location: locationnorth
  properties: {
    serverFarmId: appServicePlanNorth.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: '8.0'
	    metadata: [{
                name : 'CURRENT_STACK'
                value : 'dotnetcore'
            }]
    }
  }
}

resource appServicen2 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteNameNorth2
  location: locationnorth
  properties: {
    serverFarmId: appServicePlanNorth.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: '8.0'
	    metadata: [{
                name : 'CURRENT_STACK'
                value : 'dotnetcore'
            }]
    }
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: logAnalyticsWorkspaceName
  location: resourceGroup().location
  properties: {
    retentionInDays: 90
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: resourceGroup().location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}
