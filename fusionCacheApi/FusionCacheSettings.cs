using ZiggyCreatures.Caching.Fusion;

namespace fusionCacheApi
{
    public class FusionCacheSettingConfig
    {
        public FusionCacheEntryOptions DefaultFusionCacheEntryOptions { get; init; } = new();

        public List<FusionCacheEntrySettingsConfig> CustomCacheSettings { get; init; } = new();
    }

    public class FusionCacheEntrySettingsConfig 
    {
        public string Name { get; init; } = "";

        public FusionCacheEntryOptions? FusionCacheEntryOptions { get; init; }


    }
}
