using System.Runtime.CompilerServices;
using Anivia.CommandModules;
using Victoria;
using Victoria.Player;

namespace Anivia.Extensions;

public static class LavaNodeExtensions
{
    private static readonly ConditionalWeakTable<LavaPlayer<LavaTrack>, LavaPlayerExtension> Extensions = new();

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

    public static CustomLavalinkQueue GetQueue(this LavaPlayer<LavaTrack> lavaPlayer)
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