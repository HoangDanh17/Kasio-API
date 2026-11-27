using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Kasio.Infrastructure.Persistence.ReadModel;

public static class DependencyInjection
{
    public static IServiceCollection AddReadModel(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Mongo:ConnectionString"]
            ?? throw new InvalidOperationException("Configuration 'Mongo:ConnectionString' is not set.");
        var databaseName = configuration["Mongo:Database"]
            ?? throw new InvalidOperationException("Configuration 'Mongo:Database' is not set.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        return services;
    }
}
