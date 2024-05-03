using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace fusionCacheApi;
public interface IFusionCacheWrapper
{
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, string cacheEntryTypeName);
}

public class FusionCacheWrapper : IFusionCacheWrapper
{
    private readonly IFusionCache _fusionCache;
    private readonly FusionCacheSettingConfig _fusionCacheSettingConfig;

    public FusionCacheWrapper(IFusionCache fusionCache, IOptions<FusionCacheSettingConfig> fusionCacheSettingConfig)
    {
        _fusionCache = fusionCache;
        _fusionCacheSettingConfig = fusionCacheSettingConfig.Value;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, string cacheEntryTypeName)
    {
        if (!string.IsNullOrEmpty(cacheEntryTypeName))
        {
            var option = _fusionCacheSettingConfig.CustomCacheSettings.SingleOrDefault(c => c.Name == cacheEntryTypeName);
            ArgumentNullException.ThrowIfNull(option);
            ArgumentNullException.ThrowIfNull(option.FusionCacheEntryOptions);
            var ret = await _fusionCache.GetOrSetAsync<T>(key, factory, option.FusionCacheEntryOptions);
            return ret;
        }
        else
        {
            var ret = await _fusionCache.GetOrSetAsync<T>(key, factory);
            return ret;
        }
    }
}