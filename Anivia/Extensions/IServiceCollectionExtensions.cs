using Anivia.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anivia.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection ConfigureAuditableOptions<T>(
        this IServiceCollection serviceCollection,
        IConfigurationSection configurationSection) where T : class, new()
    {
        serviceCollection.Configure<T>(configurationSection);

        serviceCollection.AddTransient<IAuditableOptionsSnapshot<T>>(
            provider => new AuditableOptionsSnapshot<T>(
                provider.GetRequiredService<IOptionsMonitor<T>>(),
                configurationSection,
                provider.GetRequiredService<IHostEnvironment>()));

        return serviceCollection;
    }
}