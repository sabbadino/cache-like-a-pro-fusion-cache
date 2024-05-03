using fusionCacheApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace fusionCacheApi.Controllers
{



    [ApiController]
    [Route("[controller]")]
    public class MyController : ControllerBase
    {
     
    
       
        private readonly IDataSources _dataSources;
        private readonly IFusionCacheWrapper _fusionCacheWrapper;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private const string CacheEntryType1 = "cacheentrytype1";

        public MyController(IDataSources dataSources, IFusionCacheWrapper fusionCacheWrapper
            , IMemoryCache memoryCache,IDistributedCache distributedCache)
        {
            _dataSources = dataSources;
            _fusionCacheWrapper = fusionCacheWrapper;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        [HttpGet(template: "current-time-fail-safe", Name = "GetCurrentTimeFailSafe")]
        public async Task<string> GetCurrentTimeFailSafe(string location, bool throwEx)
        {
            async Task<string> Factory(CancellationToken _)
            {
                return await _dataSources.GetCurrentTime(location, throwEx);
            }

            var ret = await _fusionCacheWrapper.GetOrSetAsync(location, Factory, CacheEntryType1);
            return ret;  
        }

        [HttpGet(template: "current-time-in-memory", Name = "GetCurrentTimeInMemory")]
        public async Task<string> GetCurrentTimeInMemory(string location, bool throwEx)
        {
            var ret = await _memoryCache.GetOrCreateAsync(location, async cacheEntry=>
            {
                if (throwEx)
                {
                    throw new Exception("forced exception");
                }
                cacheEntry.AbsoluteExpiration = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
                return await _dataSources.GetCurrentTime(location, throwEx);
            });
            return ret;
        }

        [HttpGet(template: "current-time-redis", Name = "GetCurrentTimeRedis")]
        public async Task<string> GetCurrentTimeRedis(string location, bool throwEx)
        {
            var ret = await _distributedCache.GetStringAsync(location);
            if(ret == null)
            {
                if (throwEx)
                {
                    throw new Exception("forced exception");
                }
                ret = await _dataSources.GetCurrentTime(location, throwEx);
                await _distributedCache.SetStringAsync(location, ret, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)});
            }
            return ret;
        }
        private readonly string _portKey = "allPorts";
        private static Random _random = new Random();
        [HttpGet(template: "get-ports-in-memory", Name = "GetPortsInMemory")]
        public async Task<double> GetPortsInMemory(int iterations, bool throwEx)
        {
            var dt = DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                var portStartsWith = Convert.ToChar(_random.Next(15, 23)).ToString();
                var ret = await _memoryCache.GetOrCreateAsync(_portKey, async cacheEntry =>
                {
                    if (throwEx)
                    {
                        throw new Exception("forced exception");
                    }
                    cacheEntry.AbsoluteExpiration = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
                    return await _dataSources.GetPorts(throwEx);
                });
                _ = (ret ?? []).Where(p => p.LongDisplayName.StartsWith(portStartsWith, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return (DateTime.Now- dt).TotalSeconds;
        }

        [HttpGet(template: "get-ports-redis", Name = "GetPortsRedis")]
        public async Task<double> GetPortsRedis(int iterations, bool throwEx)
        {
            var dt = DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                List<PortDetails>? ports ;
                var str = await _distributedCache.GetStringAsync(_portKey);
                if (str == null)
                {
                    if (throwEx)
                    {
                        throw new Exception("forced exception");
                    }
                    ports = await _dataSources.GetPorts(throwEx);
                    await _distributedCache.SetStringAsync(_portKey, JsonSerializer.Serialize(ports), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) });
                }
                else
                {
                    ports = JsonSerializer.Deserialize<List<PortDetails>>(str);
                }
                var portStartsWith = Convert.ToChar(_random.Next(15, 23)).ToString();
                
                _ = (ports ?? []).Where(p => p.LongDisplayName.StartsWith(portStartsWith, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return (DateTime.Now - dt).TotalSeconds;
        }

    }
}
