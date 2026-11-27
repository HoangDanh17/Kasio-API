using Kasio.Application.Common.Interfaces;
using Kasio.Infrastructure.Cache.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kasio.Infrastructure.Cache;

public static class DependencyInjection
{
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Redis.");

        services.AddSingleton(new RedisConnection(redis));
        services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();

        return services;
    }
}
