@description('Specify the name of the Azure Redis Cache to create.')
param redisCacheName string = 'fusion-redis'

@description('Location of all resources')
param location string = 'francecentral' 

param redisCacheSKU string = 'Standard'

param redisCacheFamily string = 'C'

param redisCacheCapacity int = 1

param enableNonSslPort bool = false

resource redisCache 'Microsoft.Cache/Redis@2020-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: '1.2'
    sku: {
      capacity: redisCacheCapacity
      family: redisCacheFamily
      name: redisCacheSKU
    }
  }
}


