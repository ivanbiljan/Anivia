using System.Runtime.InteropServices;
using Anivia.Infrastructure;
using Discord.Commands;
using Lavalink4NET;

namespace Anivia.CommandModules;

[Name("Queue State")]
[Summary("Commands used for updating the state of the queue")]
public sealed class QueueStateCommands(IAudioService lavalinkAudioService) : AniviaCommandModule(lavalinkAudioService)
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    
    [Command("shuffle")]
    [RequireUserInVoiceChannel]
    public async Task ShuffleQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        await player.Queue.ShuffleAsync();
        await ReplyAsync(embed: Embeds.Success("The queue has been shuffled"));
    }

    [Command("skip")]
    [Summary("Skips the current track, or an arbitrary number of tracks")]
    [Remarks("skip [number of tracks]")]
    [RequireUserInVoiceChannel]
    public async Task SkipAsync([Optional] int? numberOfTracks)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        await player.SkipAsync(numberOfTracks ?? 1);
    }

    [Command("jump to")]
    [Summary("Jumps to the desired track")]
    [Remarks("jump to <position>")]
    [RequireUserInVoiceChannel]
    public async Task SkipToAsync(int position)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var queueIndex = position - 1;
        if (queueIndex < 0 || queueIndex >= player.Queue.Count)
        {
            return;
        }
        
        await player.PlayAsync(player.Queue[queueIndex]);
        await ReplyAsync(embed: Embeds.Success($"Jumped to track at position {position}"));
    }
}