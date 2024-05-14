using fusionCacheApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace fusionCacheApi.Controllers
{



    [ApiController]
    [Route("[controller]")]
    public class SimpleCacheController : ControllerBase
    {
     
    
       
        private readonly IDataSources _dataSources;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        public SimpleCacheController(IDataSources dataSources, IMemoryCache memoryCache,IDistributedCache distributedCache)
        {
            _dataSources = dataSources;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
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
        [HttpGet(template: "get-big-cache-payload-in-memory", Name = "GetBigCachePayloadInMemory")]
        public async Task<double> GetBigCachePayloadInMemory(int iterations, bool throwEx)
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

        [HttpGet(template: "get-big-cache-payload-redis", Name = "GetBigCachePayloadRedis")]
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
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        [HttpGet(template: "no-cache-stampede", Name = "NoCacheStampede")]
        public async Task<CacheStampedeResponse> NoCacheStampede(int sleepInSeconds)
        {
            var ret = await _memoryCache.GetOrCreateAsync("no-cache-stampede", async cacheEntry => {

                await _lock.WaitAsync();
                Counter.Count = Interlocked.Increment(ref Counter.Count);
                Console.WriteLine($"Entered in factory  {Counter.Count} times ");
                _lock.Release();
                await Task.Delay(sleepInSeconds*1000);
                cacheEntry.AbsoluteExpiration = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
                return await Task.FromResult(Guid.NewGuid().ToString());
            });
            return new CacheStampedeResponse {  Value = ret };
        }
    }

    public class Counter
    {
        public static int Count;
    }
        public class CacheStampedeResponse
    {
        public string Value { get; init; } = "";
      
    }
}
