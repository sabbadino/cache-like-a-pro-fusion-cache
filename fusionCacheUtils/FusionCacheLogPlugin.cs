using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Events;
using ZiggyCreatures.Caching.Fusion.Plugins;

namespace fusionCacheUtils;

public class FusionCacheLogPlugin : IFusionCachePlugin
{
    private readonly ILogger<FusionCacheLogPlugin> _logger;

    public FusionCacheLogPlugin(ILogger<FusionCacheLogPlugin> logger)
    {
        _logger = logger;
    }

    public void Start(IFusionCache cache)
    {
        cache.Events.FailSafeActivate += OnFailSafeActivate;
        cache.Events.Hit += OnHit;
        cache.Events.Miss += OnMiss;
    }

    private void OnMiss(object? sender, FusionCacheEntryEventArgs e)
    {
        _logger.LogWarning($"OnMiss {e.Key}");
    }

    private void OnHit(object? sender, FusionCacheEntryHitEventArgs e)
    {
        _logger.LogWarning($"OnHit {e.Key}");
    }


    public void Stop(IFusionCache cache)
    {
        cache.Events.FailSafeActivate -= OnFailSafeActivate;
    }

    private void OnFailSafeActivate(object? sender, FusionCacheEntryEventArgs e)
    {
        _logger.LogWarning($"OnFailSafeActivate {e.Key}");
    }
}