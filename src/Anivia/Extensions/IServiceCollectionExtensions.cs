using Anivia.Infrastructure;
using Microsoft.Extensions.Options;

namespace Anivia.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection ConfigureAuditableOptions<T>(
        this IServiceCollection serviceCollection,
        IConfigurationSection configurationSection
    ) where T : class, new()
    {
        serviceCollection.Configure<T>(configurationSection);

        serviceCollection.AddTransient<IAuditableOptionsSnapshot<T>>(
            provider => new AuditableOptionsSnapshot<T>(
                provider.GetRequiredService<IOptionsMonitor<T>>(),
                configurationSection,
                provider.GetRequiredService<IHostEnvironment>()
            )
        );

        return serviceCollection;
    }
}