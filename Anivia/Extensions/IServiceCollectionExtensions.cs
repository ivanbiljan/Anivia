using System.Runtime.CompilerServices;
using Anivia.CommandModules;
using Anivia.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Victoria;

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

public static class StringExtensions
{
    public static string AsBold(this string source) => $"**{source}**";

    public static string AsItalic(this string source) => $"_{source}_";

    public static string WithUnderline(this string source) => $"__{source}__";
}

public static class LavaNodeExtensions
{
    private static readonly ConditionalWeakTable<LavaPlayer, LavaPlayerExtension> Extensions = new();

    public static CustomLavalinkQueue GetQueue(this LavaPlayer lavaPlayer)
    {
        if (Extensions.TryGetValue(lavaPlayer, out var extension))
        {
            return extension.Queue;
        }

        extension = new LavaPlayerExtension();
        Extensions.AddOrUpdate(lavaPlayer, extension);

        return extension.Queue;
    }

    public static LavaPlayerExtension GetExtension(this LavaPlayer lavaPlayer)
    {
        if (Extensions.TryGetValue(lavaPlayer, out var extension))
        {
            return extension;
        }

        extension = new LavaPlayerExtension();
        Extensions.AddOrUpdate(lavaPlayer, extension);

        return extension;
    }
}