using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
    private static readonly Regex DiscordMarkdownRegex = new("([*,_])");

    public static string AsBold(this string source) => $"**{DiscordMarkdownRegex.Replace(source, "\\$1")}**";

    public static string AsItalic(this string source) => $"_{DiscordMarkdownRegex.Replace(source, "\\$1")}_";

    public static int ComputeDistanceTo(this string source, string reference)
    {
        if (source.Length == 0)
        {
            return reference.Length;
        }

        if (reference.Length == 0)
        {
            return source.Length;
        }

        var dp = new int[source.Length + 1, reference.Length + 1];

        for (var i = 0; i <= source.Length; ++i)
        {
            dp[i, 0] = i;
        }

        for (var j = 0; j <= reference.Length; ++j)
        {
            dp[0, j] = j;
        }
        
        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= reference.Length; j++)
            {
                var cost = char.ToLower(reference[j - 1]) == char.ToLower(source[i - 1]) ? 0 : 1;
                
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }
        
        return dp[source.Length, reference.Length];
    }

    public static string WithUnderline(this string source) => $"__{DiscordMarkdownRegex.Replace(source, "\\$1")}__";
}

public static class LavaNodeExtensions
{
    private static readonly ConditionalWeakTable<LavaPlayer, LavaPlayerExtension> Extensions = new();

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
}