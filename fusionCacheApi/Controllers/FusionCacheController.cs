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
        public async Task SetCacheEntry(string value)
        {
            await _fusionCacheWrapper.SetAsync("cache-entry", value, CacheEntryTypeWithFailSafe);
        }

        [HttpGet(template: "get-or-set-cache-entry", Name = "GetOrSetCacheEntry")]
        public async Task<string> GetCacheEntry(string value)
        {
            async Task<string> Factory(CancellationToken _)
            {
                return await Task.FromResult(value);
            }

            var ret = await _fusionCacheWrapper.GetOrSetAsync("cache-entry", Factory, CacheEntryTypeWithFailSafe);
            return ret;
        }

        [HttpGet(template: "get-or-set-cache-entry-raw", Name = "GetOrSetCacheEntryRaw")]
        public async Task<string> GetCacheEntryRaw(string value)
        {
            var ret = await _fusionCache.GetOrSetAsync("cache-entry", async _ => await Task.FromResult(value),new FusionCacheEntryOptions
            {
                Duration= TimeSpan.FromMinutes(1)   
            });
            return ret;
        }

        [HttpGet(template: "get-or-set-cache-entry-raw-hard-timeout", Name = "GetOrSetCacheEntryRawHardTimeOut")]
        public async Task<string> GetOrSetCacheEntryRawTimeOut(string value, int factorySleepInSeconds)
        {
            var ret = await _fusionCache.GetOrSetAsync("get-or-set-cache-entry-raw-timeout-no-fail-safe",
                async _ => {
                    await Task.Delay(factorySleepInSeconds * 1000);
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

    [HttpGet(template: "get-or-set-cache-entry-raw-fails-safe", Name = "GetOrSetCacheEntryRawFailSafe")]
    public async Task<string> GetOrSetCacheEntryRawFailSafe(string value, bool throwEx, int factorySleepInSeconds)
    {
        // 1) call with factorySleepInSeconds > FactoryHardTimeout = 10 (15) .. it will return exception
        // 2) call again after more 5 sec .. factory has finished ... you will get the value you requested 
        // 3) lets wait expiration of entry (15 secs) then call with factorySleepInSeconds = 1 , throw ex = true .. with different value ..
        // you will get the old value (Fail safe)
        // you will see exception in the logs
        // 4) let wait expiration of entry (15 secs) factorySleepInSeconds =1 , throw ex = false .. with different value ..
        // you will get the new value 
        // when FailSafeMaxDuration expires .. you are back to point 1 
        var ret = await _fusionCache.GetOrSetAsync("get-or-set-cache-entry-raw-fails-safe",
                        async _ => {
                            if (throwEx)
                            {
                                throw new Exception("Exception");
                            }
                            await Task.Delay(factorySleepInSeconds * 1000);
                            return await Task.FromResult(value);
                        }
                        , new FusionCacheEntryOptions
                         {
                    Duration = TimeSpan.FromSeconds(15),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1)
                    //FailSafeThrottleDuration
                    ,
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
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
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

        [HttpGet(template: "reset-factory-counter", Name = "ResetFactoryCounter")]
        public void ResetFactoryCounter(int sleepInSeconds)
        {
            Counter.Count = 0;
        }
}
