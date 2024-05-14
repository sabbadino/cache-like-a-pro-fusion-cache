using ZiggyCreatures.Caching.Fusion;

namespace fusionCacheUtils
{
    public class FusionCacheSettingConfig
    {
        // just for demo purposes
        public bool RegisterRedisDistributedExplicitly { get; init; } = true;
        // just for demo purposes
        public bool RegisterInMemoryExplicitly { get; init; } = true;

        public bool EnableBackPlane { get; set; } 
        public FusionCacheEntryOptions DefaultFusionCacheEntryOptions { get; init; } = new();

        public List<FusionCacheEntrySettingsConfig> CustomCacheSettings { get; init; } = new();
    }

    public class FusionCacheEntrySettingsConfig 
    {
        public string Name { get; init; } = "";

        public FusionCacheEntryOptions? FusionCacheEntryOptions { get; init; }


    }
}
