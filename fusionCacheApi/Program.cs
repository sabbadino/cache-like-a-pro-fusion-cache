using fusionCacheApi.Repository;
using fusionCacheUtils;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using static System.Collections.Specialized.BitVector32;

namespace fusionCacheApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "local"}", Version = "v1.0" });
            });

                builder.Services.AddSingleton<IDataSources, DataSources>();
            builder.Services.AddMemoryCache();

            builder.Services.AddOptions<FusionCacheSettingConfig>().BindConfiguration(nameof(FusionCacheSettingConfig));

            var section = builder.Configuration.GetSection(nameof(FusionCacheSettingConfig));
            var fusionCacheSettingConfig = section.Get<FusionCacheSettingConfig>();
            ArgumentNullException.ThrowIfNull(fusionCacheSettingConfig);
            if (fusionCacheSettingConfig.RegisterInMemoryExplicitly)
            {
                builder.Services.AddMemoryCache(o =>
                {
                    o.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
                });
            }
            if (fusionCacheSettingConfig.RegisterRedisDistributedExplicitly)
            {
                builder.Services.AddStackExchangeRedisCache(a => {a.Configuration = builder.Configuration["redis:connectionString"];});
            }
            var fusionCacheBuilder = builder.Services.AddFusionCache().WithDefaultEntryOptions(fusionCacheSettingConfig.DefaultFusionCacheEntryOptions)
                // ADD JSON.NET BASED SERIALIZATION FOR FUSION CACHE
                .WithSerializer(
                    new FusionCacheSystemTextJsonSerializer()
                )
                // ADD REDIS DISTRIBUTED CACHE SUPPORT
                .WithDistributedCache(
                    new RedisCache(new RedisCacheOptions { InstanceName = "fusionCacheApi", Configuration = builder.Configuration["redis:connectionString"] })
                );

            // ADD THE FUSION CACHE BACKPLANE FOR REDIS
            if (fusionCacheSettingConfig.EnableBackPlane)
            {
                fusionCacheBuilder.WithBackplane(
                        new RedisBackplane(new RedisBackplaneOptions() { Configuration = builder.Configuration["redis:connectionString"] })
                    );
            }

            builder.Services.AddSingleton<IFusionCacheWrapper, FusionCacheWrapper>();

            var app = builder.Build();

           
            app.UseSwagger();
            app.UseSwaggerUI();
          

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
