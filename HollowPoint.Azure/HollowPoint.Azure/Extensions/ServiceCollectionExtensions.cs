using Azure.Data.Tables;
using HollowPoint.Azure.Configuration;
using HollowPoint.Azure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HollowPoint.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHollowPointAzure(
        this IServiceCollection services,
        Action<AzureTableOptions> configure)
    {
        services.Configure(configure);
        services.AddMemoryCache();

        services.AddSingleton<TableServiceClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureTableOptions>>().Value;

            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new InvalidOperationException(
                    "Azure Storage connection string is required. " +
                    "Configure it using AddHollowPointAzure(options => options.ConnectionString = \"...\")");

            return new TableServiceClient(options.ConnectionString);
        });

        services.AddScoped(typeof(IDbTable<>), typeof(DbTable<>));

        return services;
    }
}
