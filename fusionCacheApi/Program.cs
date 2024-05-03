using fusionCacheApi.Repository;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
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
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton<IDataSources, DataSources>();
            builder.Services.AddMemoryCache();

            builder.Services.AddOptions<FusionCacheSettingConfig>().BindConfiguration(nameof(FusionCacheSettingConfig)).Validate(config =>
            {
                return true;
            }).ValidateOnStart();

            var section = builder.Configuration.GetSection(nameof(FusionCacheSettingConfig));
            var fusionCacheSettingConfig = section.Get<FusionCacheSettingConfig>();
            ArgumentNullException.ThrowIfNull(fusionCacheSettingConfig);

            builder.Services.AddMemoryCache(o =>
            {
                o.ExpirationScanFrequency = TimeSpan.FromSeconds(1);
            });
            builder.Services.AddStackExchangeRedisCache(a =>
            {
                a.Configuration = builder.Configuration["redis:connectionString"];
            });
            builder.Services.AddFusionCache().WithDefaultEntryOptions(fusionCacheSettingConfig.DefaultFusionCacheEntryOptions)
                // ADD JSON.NET BASED SERIALIZATION FOR FUSION CACHE
                .WithSerializer(
                    new FusionCacheSystemTextJsonSerializer()
                )
                // ADD REDIS DISTRIBUTED CACHE SUPPORT
                .WithDistributedCache(
                    new RedisCache(new RedisCacheOptions {  InstanceName= "fusionCacheApi", Configuration = builder.Configuration["redis:connectionString"] })
                )  // ADD THE FUSION CACHE BACKPLANE FOR REDIS
                .WithBackplane(
                    new RedisBackplane(new RedisBackplaneOptions() { Configuration = builder.Configuration["redis:connectionString"] })
                );
            
            builder.Services.AddSingleton<IFusionCacheWrapper, FusionCacheWrapper>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
