using fusionCacheApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using fusionCacheUtils;
using ZiggyCreatures.Caching.Fusion;

namespace fusionCacheApi.Controllers
{



    [ApiController]
    [Route("[controller]")]
    public class FusionCacheController : ControllerBase
    {
     
    
       
        private readonly IDataSources _dataSources;
        private readonly IFusionCacheWrapper _fusionCacheWrapper;
        private const string CacheEntryTypeWithFailSafe = "CacheEntryTypeWithFailSafe";
        private const string CacheEntryTypeNoFailSafe = "CacheEntryTypeNoFailSafe";

        public FusionCacheController(IDataSources dataSources, IFusionCacheWrapper fusionCacheWrapper)
        {
            _dataSources = dataSources;
            _fusionCacheWrapper = fusionCacheWrapper;
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

    }
}
