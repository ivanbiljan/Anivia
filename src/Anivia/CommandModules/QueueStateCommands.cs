using Anivia.Infrastructure;
using Lavalink4NET;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class QueueStateCommands(IAudioService lavalinkAudioService)
    : AniviaCommandModule(lavalinkAudioService)
{

    [AniviaCommand("shuffle", "Queue state", "Shuffles the order of tracks in the queue")]
    [RequireUserInVoiceChannel]
    public async Task ShuffleQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        player.Queue.Shuffle();
        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success("The queue has been shuffled")]});
    }

    [AniviaCommand("skip", "Queue state", "Skips the current track, or an arbitrary number of tracks")]
    [RequireUserInVoiceChannel]
    public async Task SkipAsync([CommandParameter] int numberOfTracks = 1)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        await player.SkipAsync(numberOfTracks);
    }

    [AniviaCommand("jumpto", "Queue state", "Jumps to the desired track")]
    [RequireUserInVoiceChannel]
    public async Task SkipToAsync(int position)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var nextTrack = player.Queue[position - 1];
        if (nextTrack is null)
        {
            return;
        }

        await player.JumpToAsync(position - 1);
        await ReplyAsync(
            new ReplyMessageProperties {Embeds = [Embeds.Success($"Jumped to track at position {position}")]}
        );
    }
}