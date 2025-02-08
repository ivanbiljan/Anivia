﻿using System.Runtime.InteropServices;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria.Node;

namespace Anivia.CommandModules;

[Name("Queue State")]
[Summary("Commands used for updating the state of the queue")]
public sealed class QueueStateModule(LavaNode lavaNode) : ModuleBase
{
    private readonly LavaNode _lavaNode = lavaNode;

    [Command("shuffle")]
    public async Task ShuffleQueueAsync()
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        player.GetQueue().Shuffle();

        await ReplyAsync(embed: Embeds.Success("The queue has been shuffled"));
    }

    [Command("skip")]
    [Summary("Skips the current track, or an arbitrary number of tracks")]
    [Remarks("skip [number of tracks]")]
    public async Task SkipAsync([Optional] int? numberOfTracks)
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        if (numberOfTracks.HasValue)
        {
            player.GetQueue().SkipTracks(numberOfTracks.Value - 1);
        }

        await player.StopAsync();
    }

    [Command("jump to")]
    [Summary("Jumps to the desired track")]
    [Remarks("jump to <position>")]
    public async Task SkipToAsync(int trackIndex)
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        player.GetQueue().JumpToTrack(trackIndex - 1);

        await player.StopAsync();
        await ReplyAsync(embed: Embeds.Success($"Jumped to track at position {trackIndex}"));
    }
}