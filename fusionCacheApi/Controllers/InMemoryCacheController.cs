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
    public class InMemoryCacheController : ControllerBase
    {
     
    
        private readonly string _portKey = "allPorts";
        private static readonly Random Random = new   ();
        private readonly IDataSources _dataSources;
        private readonly IMemoryCache _memoryCache;

        public InMemoryCacheController(IDataSources dataSources, IMemoryCache memoryCache,ILogger<InMemoryCacheController> logger)
        {
            _dataSources = dataSources;
            _memoryCache = memoryCache;
            logger.LogInformation("enter");
        }

        // 2)  this show calling mem cache many time on big payload .. compare with same method in distributed cache 
        [HttpGet(template: "get-big-cache-payload-in-memory", Name = "GetBigCachePayloadInMemory")]
        public async Task<double> GetBigCachePayloadInMemory(int iterations)
        {
            var dt = DateTime.Now;
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10
            };
            await Parallel.ForEachAsync(new int[iterations], parallelOptions, async (iteration, token) =>
                {
                    var portStartsWith = Convert.ToChar(Random.Next(15, 23)).ToString();
                    var ret = await _memoryCache.GetOrCreateAsync(_portKey, async cacheEntry =>
                    {
                        cacheEntry.AbsoluteExpiration = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
                        return await _dataSources.GetPorts();
                    });
                    _ = (ret ?? []).Where(p => p.LongDisplayName.StartsWith(portStartsWith, StringComparison.OrdinalIgnoreCase)).ToList();
                });
            return (DateTime.Now- dt).TotalSeconds;
        }



        [HttpGet(template: "reset-in-memory-factory-counter", Name = "ResetInMemoryFactoryCounter")]
        public void ResetFactoryCounter()
        {
            Counter.Count = 0;
        }

        [HttpGet(template: "get-in-memory-factory-counter", Name = "GetInMemoryFactoryCounter")]
        public int GetFactoryCounter()
        {
            return Counter.Count;
        }

        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        // 1) this is to show that in-memory cache does not protect from cache stampede
        [HttpGet(template: "cache-stampede-in-memory", Name = "CacheStampedeInMemory")]
        public async Task<CacheStampedeResponse> CacheStampede(int sleepInSeconds)
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
            return new CacheStampedeResponse {  Value = ret  };
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

    
}
