using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace Anivia.CommandModules;

[Name("Playback")]
public sealed class PlaybackModule : ModuleBase
{
    private readonly LavaNode _lavaNode;

    public PlaybackModule(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
    }

    [Command("play")]
    [Summary("Queue a track or playlist from a search term or url")]
    public async Task PlayAsync([Remainder] string song)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }
        
        if (_lavaNode.HasPlayer(Context.Guild) && _lavaNode.GetPlayer(Context.Guild).VoiceChannel != voiceState.VoiceChannel)
        {
            await ReplyAsync(embed: Embeds.Error("Music is already playing"));

            return;
        }

        var player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, (ITextChannel)Context.Channel);
        var searchResponse = await _lavaNode.SearchAsync(SearchType.YouTube, song);
        var track = searchResponse.Tracks.First();

        var queue = player.GetQueue();
        queue.Add(track);
        
        if (queue.Length > 1)
        {
            var embed = new EmbedBuilder().WithTitle("Added Track")
                .AddField("Track", $"[{track.Title}]({track.Url})")
                // .AddField("Estimated time until played", "n")
                .AddField("Track length", track.Duration, true)
                // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                .AddField("Position in queue", queue.Length, true)
                .WithFooter($"Requested by {Context.User.Username}")
                .Build();

            await ReplyAsync(embed: embed);
        }

        if (player.PlayerState is PlayerState.None or PlayerState.Stopped)
        {
            await player.PlayAsync(queue.ConsumeAndAdvance());
        }
    }

    [Command("remove")]
    [Alias("rm", "delete", "del")]
    public async Task RemoveAsync(int index)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }
        
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetQueue();
        var track = queue.Remove(index - 1);

        await ReplyAsync(embed: Embeds.Success($"Removed track [{track.Title}]({track.Url})"));
    }

    [Command("pause")]
    [Summary("Pauses the current track")]
    public async Task PauseAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }
        
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.PauseAsync();
    }

    [Command("resume")]
    public async Task ResumeAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }
        
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.ResumeAsync();
    }
    
    [Command("skip")]
    public async Task SkipAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }
        
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.SkipAsync();
    }

    [Command("queue")]
    public async Task DisplayQueueAsync()
    {
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetQueue();
        var embed = new EmbedBuilder()
            .WithDescription(
                string.Join(
                    Environment.NewLine,
                    queue.Select(
                        (track, index) =>
                            $"{index + 1} - `[{track.Duration}]` [{track.Title.AsBold()}]({track.Url})")))
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("seek")]
    public async Task SeekAsync(string timestamp)
    {
        var player = _lavaNode.GetPlayer(Context.Guild);

        await player.SeekAsync(TimeSpan.Parse(timestamp));
    }

    [Command("forward")]
    public async Task ForwardAsync(int seconds)
    {
        var player = _lavaNode.GetPlayer(Context.Guild);

        var forwardedTimestamp = player.Track.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);
    }
    
    [Command("back")]
    public async Task BackAsync(int seconds)
    {
        var player = _lavaNode.GetPlayer(Context.Guild);

        var forwardedTimestamp = player.Track.Position.Subtract(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);
    }

    [Command("insert")]
    [Summary("Insert a track right after the one that is currently playing")]
    public async Task InsertAsync()
    {
    }

    [Command("leave")]
    [Summary("Makes me leave the voice channel")]
    public async Task LeaveAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
    }
}