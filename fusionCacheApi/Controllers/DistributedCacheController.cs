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
        private readonly string _portKey = "allPorts";
        private static readonly Random Random = new();
     

        //  3) this show calling mem cache many time on big payload .. compare with same method in distributed cache 
        [HttpGet(template: "get-big-cache-payload-redis", Name = "GetBigCachePayloadRedis")]
        public async Task<double> GetPortsRedis(int iterations)
        {
            var dt = DateTime.Now;
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism =10
            };
            await Parallel.ForEachAsync(new int[iterations], parallelOptions, async (iteration,token)  =>
            {
                List<PortDetails>? ports ;
                var str = await _distributedCache.GetStringAsync(_portKey);
                if (str == null)
                {
                    ports = await _dataSources.GetPorts();
                    await _distributedCache.SetStringAsync(_portKey, JsonSerializer.Serialize(ports), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) },token);
                }
                else
                {
                    ports = JsonSerializer.Deserialize<List<PortDetails>>(str);
                }
                var portStartsWith = Convert.ToChar(Random.Next(15, 23)).ToString();
                
                _ = (ports ?? []).Where(p => p.LongDisplayName.StartsWith(portStartsWith, StringComparison.OrdinalIgnoreCase)).ToList();
            });
            return (DateTime.Now - dt).TotalSeconds;
        }
      
       

    }

   
}
