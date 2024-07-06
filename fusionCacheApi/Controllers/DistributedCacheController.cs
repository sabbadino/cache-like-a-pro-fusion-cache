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
    public class DistributedCacheController : ControllerBase
    {
     
    
       
        private readonly IDataSources _dataSources;
        private readonly IDistributedCache _distributedCache;

        public DistributedCacheController(IDataSources dataSources,IDistributedCache distributedCache)
        {
            _dataSources = dataSources;
            _distributedCache = distributedCache;
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
        private static readonly Random Random = new ();

        //  3) this show calling mem cache many time on big payload .. compare with same method in distributed cache 
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
                var portStartsWith = Convert.ToChar(Random.Next(15, 23)).ToString();
                
                _ = (ports ?? []).Where(p => p.LongDisplayName.StartsWith(portStartsWith, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return (DateTime.Now - dt).TotalSeconds;
        }
      
       

    }

   
}
