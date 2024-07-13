using fusionCacheApi.Repository;
using fusionCacheUtils;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using static System.Collections.Specialized.BitVector32;

namespace fusionCacheApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            SetUpApplicationInsights(builder);

            builder.Services.AddControllers();
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c => {
                //     c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "local"}", Version = "v1.0" });
                c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{builder.Environment.EnvironmentName}", Version = "v1.0" });
            });

            builder.Services.AddSingleton<IDataSources, DataSources>();

            builder.Services.AddMemoryCache();

            builder.Services.AddOptions<FusionCacheSettingConfig>().BindConfiguration(
                nameof(FusionCacheSettingConfig));

            var section = builder.Configuration.GetSection(nameof(FusionCacheSettingConfig));
            var fusionCacheSettingConfig = section.Get<FusionCacheSettingConfig>();
            ArgumentNullException.ThrowIfNull(fusionCacheSettingConfig);

            if (fusionCacheSettingConfig.RegisterRedisDistributedExplicitly) {
                builder.Services.AddStackExchangeRedisCache(
                    opt => {
                            opt.Configuration = builder.Configuration["redis:connectionString"];
                        }
                    );
            }
            var fusionCacheBuilder = builder.Services.AddFusionCache()
                .WithDefaultEntryOptions(
                fusionCacheSettingConfig.DefaultFusionCacheEntryOptions)
                // ADD JSON.NET BASED SERIALIZATION FOR FUSION CACHE
                .WithSerializer(
                    new FusionCacheSystemTextJsonSerializer()
                )
                // ADD REDIS DISTRIBUTED CACHE SUPPORT
                .WithDistributedCache(
                    new RedisCache(new RedisCacheOptions { 
                        InstanceName = "fusionCacheApi", 
                        Configuration = builder.Configuration["redis:connectionString"] })
                );

            // ADD THE FUSION CACHE BACKPLANE FOR REDIS
            if (fusionCacheSettingConfig.EnableBackPlane) {
                fusionCacheBuilder.WithBackplane(
                    new RedisBackplane(
                        new RedisBackplaneOptions { 
                            Configuration = builder.Configuration["redis:connectionString"] })
                );
            }

            // setup FusionCache plugin 
            builder.Services.AddSingleton<IFusionCachePlugin, FusionCacheLogPlugin>();
            fusionCacheBuilder.WithAllRegisteredPlugins();
            builder.Services.AddSingleton<IFusionCacheWrapper, FusionCacheWrapper>();

            var app = builder.Build();

           
            app.UseSwagger();
            app.UseSwaggerUI();
          

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        private static void SetUpApplicationInsights(WebApplicationBuilder builder)
        {
            var aiCnString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            ArgumentException.ThrowIfNullOrEmpty(aiCnString);
            var aiOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
            {
                // Disables adaptive sampling.
                EnableAdaptiveSampling = false,
            };

            builder.Services.AddApplicationInsightsTelemetry(aiOptions);
           
            builder.Logging.AddApplicationInsights(
                configureTelemetryConfiguration: (config) => {
                    config.ConnectionString = aiCnString;
                }, configureApplicationInsightsLoggerOptions: (options) => { }
            );

            // to send to AI logs down to debug level 
            builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(null, LogLevel.Debug);
            builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);
        }
    }
}
