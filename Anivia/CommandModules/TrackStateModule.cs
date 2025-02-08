﻿using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria;

namespace Anivia.CommandModules;

[Name("Track State")]
[Summary("Commands used for updating the state of the current track")]
public sealed class TrackStateModule : ModuleBase
{
    private readonly LavaNode _lavaNode;

    public TrackStateModule(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
    }

    [Command("pause")]
    [Summary("Pauses the current track")]
    public async Task PauseCurrentTrackAsync()
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.PauseAsync(_lavaNode);
        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been paused at {player.Track.Position.ToShortString().AsBold()} :pause_button:"));
    }

    [Command("resume")]
    [Summary("Resumes the current track")]
    public async Task ResumeCurrentTrackAsync()
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.ResumeAsync(_lavaNode, player.Track);
        await ReplyAsync(embed: Embeds.Success("Track has been resumed :play_pause:"));
    }

    [Command("seek")]
    [Alias("wind to")]
    public async Task SeekAsync(string timestamp)
    {
        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }
        
        if (!TimeSpan.TryParse(timestamp, out var position))
        {
            await ReplyAsync(embed: Embeds.Error("Invalid timestamp"));

            return;
        }

        await player.SeekAsync(_lavaNode, position);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {position.ToShortString()}/{player.Track.Duration}"));
    }

    [Command("backward")]
    [Alias("back", "b", "rewind")]
    [Summary("Rewinds the current track by a number of seconds")]
    [Remarks("back 10")]
    public async Task WindCurrentTrackBackwardsAsync(int seconds)
    {
        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var forwardedTimestamp = player.Track.Position.Subtract(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(_lavaNode, forwardedTimestamp);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.Track.Duration} :rewind:"));
    }

    [Command("forward")]
    [Alias("f")]
    public async Task WindCurrentTrackForwardAsync(int seconds)
    {
        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var forwardedTimestamp = player.Track.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(_lavaNode, forwardedTimestamp);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.Track.Duration} :fast_forward:"));
    }
}