using fusionCacheApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using fusionCacheUtils;
using ZiggyCreatures.Caching.Fusion;

namespace fusionCacheApi.Controllers;




    [ApiController]
    [Route("[controller]")]
    public class FusionCacheController : ControllerBase
    {


    private static readonly SemaphoreSlim _lock = new (1, 1);
    private readonly IDataSources _dataSources;
        private readonly IFusionCacheWrapper _fusionCacheWrapper;
        private readonly IFusionCache _fusionCache;
        private const string CacheEntryTypeWithFailSafe = "CacheEntryTypeWithFailSafe";
        private const string CacheEntryTypeNoFailSafe = "CacheEntryTypeNoFailSafe";
        private const string CacheEntryTypeStampede = "CacheEntryTypeStampede";

        public FusionCacheController(IDataSources dataSources, IFusionCacheWrapper fusionCacheWrapper, IFusionCache fusionCache)
        {
            _dataSources = dataSources;
            _fusionCacheWrapper = fusionCacheWrapper;
            _fusionCache = fusionCache;
        }

        [HttpGet(template: "current-time-fail-safe", Name = "GetCurrentTimeFailSafe")]
        public async Task<string> GetCurrentTimeFailSafe(string location, bool throwEx)
        {
            async Task<string> Factory(CancellationToken _)
            {
                return await _dataSources.GetCurrentTime(location, throwEx);
            }

            var ret = await _fusionCacheWrapper.GetOrSetAsync(location, Factory, CacheEntryTypeWithFailSafe);
            return ret;  
        }

        [HttpGet(template: "set-cache-entry", Name = "SetCacheEntry")]
        public async Task SetCacheEntry(string key, string value)
        {
            await _fusionCacheWrapper.SetAsync(key, value, CacheEntryTypeNoFailSafe);
        }
        [HttpGet(template: "set-cache-entry-raw", Name = "SetCacheEntryRaw")]
        public async Task SetCacheEntryRaw(string key, string value)
        {
        await _fusionCache.SetAsync(key, value, new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(1)
        });
        
    }
    // simple example . show how to call the wrapper , that gets cache entry from config 
    [HttpGet(template: "get-or-set-cache-entry", Name = "GetOrSetCacheEntry")]
        public async Task<string> GetCacheEntry(string key, string value)
        {
            async Task<string> Factory(CancellationToken _)
            {
                return await Task.FromResult(value);
            }

            var ret = await _fusionCacheWrapper.GetOrSetAsync(key, Factory, CacheEntryTypeNoFailSafe);
            return ret;
        }
        // 4) just to show the simplest way to call GetOrSetAsync  
        [HttpGet(template: "get-or-set-cache-entry-raw", Name = "GetOrSetCacheEntryRaw")]
        public async Task<string> GetOrSetCacheEntryRaw(string key, string value)
        {
            var ret = await _fusionCache.GetOrSetAsync(key, async _ => await Task.FromResult(value),new FusionCacheEntryOptions
            {
                Duration= TimeSpan.FromSeconds(1)   
            });
            return ret;
        }

    [HttpGet(template: "get-cache-entry-raw", Name = "GetCacheEntryRaw")]
    public string? GetCacheEntryRaw(string key)
    {
        var ret = _fusionCache.TryGet<string>(key);
        if (ret.HasValue)
        {
            return ret.Value;
        }
        return null;
    }


    // 5) to show error when factory takes longer then FactoryHardTimeout
    // also show that factory is executed in background thread
    // call this with value of 18 .. so it will fail (timeout 15) ..but calling next time will give result (immediately)
    [HttpGet(template: "get-or-set-cache-entry-raw-hard-timeout", Name = "GetOrSetCacheEntryRawHardTimeOut")]
        public async Task<string> GetOrSetCacheEntryRawTimeOut(string key ,string value, int factorySleepInSeconds)
        {
            var ret = await _fusionCache.GetOrSetAsync(key,
                async _ => {
                    await Task.Delay(factorySleepInSeconds * 1000, _);
                    return await Task.FromResult(value);
                }, new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromSeconds(60),
                    FactoryHardTimeout = TimeSpan.FromSeconds(15),
                });
            return ret;
        }

    [HttpGet(template: "get-or-set-cache-entry-raw-fails-safe-default-value", Name = "GetOrSetCacheEntryRawFailSafeDefaultValue")]
        public async Task<string> GetOrSetCacheEntryRawFailSafeDefaultValue(string value, bool throwEx, int factorySleepInSeconds)
        {
        // 1) call with factorySleepInSeconds > FactoryHardTimeout = 10 (15) .. it will return "theDefaultValue"
        // 2) call again after more 5 sec .. factory has finished ... you will get the value you requested 
        // 3) lets wait expiration of entry (15 secs) then call with factorySleepInSeconds = 1 , throw ex = true .. with different value ..
        // you will get the old value (Fail safe)
        // you will see exception in the logs
        // 4) let wait expiration of entry (15 secs) factorySleepInSeconds =1 , throw ex = false .. with different value ..
        // you will get the new value 
        // when FailSafeMaxDuration expires .. you are back to point 1 
        var ret = await _fusionCache.GetOrSetAsync("get-or-set-cache-entry-raw-fails-safe-default-value", 
                async _ => {
                    if (throwEx)
                    {
                        throw new Exception("Exception");
                    }
                    await Task.Delay(factorySleepInSeconds * 1000);
                    return await Task.FromResult(value);
                },
                "theDefaultValue" /* this default value applies only when fail-safe is enabled */ , new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromSeconds(15), IsFailSafeEnabled=true
                , FailSafeMaxDuration=TimeSpan.FromHours(1) 
                //FailSafeThrottleDuration
                , FactoryHardTimeout = TimeSpan.FromSeconds(10)
                , FactorySoftTimeout= TimeSpan.FromSeconds(5),
                });
            return ret;
        }
    // 
    [HttpGet(template: "get-or-set-cache-entry-raw-fails-safe", Name = "GetOrSetCacheEntryRawFailSafe")]
    public async Task<string> GetOrSetCacheEntryRawFailSafe(string key, string value, bool throwEx)
    {
        // 7 fail safe 
        // call with throw ex = false 
        // call with throwEx true after 5 seconds 
        // you will get the old value (Fail safe)
        // you will see exception in the logs
        // call with throw ex = false .. with different value ..
        // you will get the new value 
        var ret = await _fusionCache.GetOrSetAsync(key,
                        async _ => {
                            if (throwEx)
                            {
                                throw new Exception("Exception");
                            }
                            return await Task.FromResult(value);
                        }
                        , new FusionCacheEntryOptions
                         {
                    Duration = TimeSpan.FromSeconds(5),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FailSafeThrottleDuration= TimeSpan.FromSeconds(1),
                    FactoryHardTimeout = TimeSpan.FromSeconds(10)
                    ,
                    FactorySoftTimeout = TimeSpan.FromSeconds(5),
                });
        return ret;
    }

           
    [HttpGet(template: "get-or-set-cache-entry-with-adaptive-cache", Name = "GetOrSetCacheEntryWithAdaptiveCache")]
        public async Task<string> GetOrSetCacheEntryWithAdaptiveCache(string value, int? durationInSeconds)
        {
            async Task<string> Factory(FusionCacheFactoryExecutionContext<string> ctx, CancellationToken _)
            {
                if (durationInSeconds != null)
                {
                    ctx.Options.Duration = TimeSpan.FromSeconds(durationInSeconds.Value);
                }
                return await Task.FromResult(value) ;
            }

            var ret = await _fusionCacheWrapper.GetOrSetAdaptiveCacheAsync<string>("cache-entry-adaptive-cache", Factory, CacheEntryTypeNoFailSafe);
            return ret;
        }
        
    // 6) how FC prevent cache stampede 
        [HttpGet(template: "cache-stampede", Name = "CacheStampedeFusion")]
        public async Task<string> CacheStampede(int sleepInSeconds)
        {
            async Task<string> Factory(CancellationToken _)
            {
                await _lock.WaitAsync();
                Counter.Count = Interlocked.Increment(ref Counter.Count);
                Console.WriteLine($"Entered in fusion cache factory  {Counter.Count} times ");
                _lock.Release();
                await Task.Delay(sleepInSeconds * 1000);
                return await Task.FromResult(Guid.NewGuid().ToString());
            }

            var ret = await _fusionCacheWrapper.GetOrSetAsync("cache-entry", Factory, CacheEntryTypeStampede);
            return ret;
        }
        [HttpGet(template: "get-factory-counter", Name = "GetFactoryCounter")]
        public int GetFactoryCounter()
        {
            return Counter.Count;
        }
    [HttpGet(template: "reset-factory-counter", Name = "ResetFactoryCounter")]
        public void ResetFactoryCounter()
        {
            Counter.Count = 0;
        }

        public class Counter
        {
            public static int Count;
        }
        
}
