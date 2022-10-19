using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria;
using Victoria.Enums;
using Victoria.Filters;
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

        var searchResponse = Uri.IsWellFormedUriString(song, UriKind.RelativeOrAbsolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, song)
            : await _lavaNode.SearchAsync(SearchType.YouTube, song);

        if (searchResponse.Tracks.Count == 0)
        {
            await ReplyAsync(embed: Embeds.Error("No tracks that match your search"));

            return;
        }
        
        var queue = player.GetQueue();
        if (searchResponse.Status == SearchStatus.PlaylistLoaded)
        {
            var track = searchResponse.Tracks.First();
            queue.Add(searchResponse.Tracks);
            
            var embed = new EmbedBuilder()
                .WithTitle("Added Playlist")
                .WithThumbnailUrl($"https://img.youtube.com/vi/{track.Id}/0.jpg")
                .AddField("Track", $"[{track.Title}]({track.Url})")
                // .AddField("Estimated time until played", "n")
                .AddField("Track length", track.Duration, true)
                // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                .AddField("Position in queue", queue.Length, true)
                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                .Build();

            await ReplyAsync(embed: embed);
        }
        else
        {
            LavaTrack bestMatch = null!;
            var lowestDistance = int.MaxValue;

            foreach (var track in searchResponse.Tracks.Take(3))
            {
                var distance = song.ComputeDistanceTo(track.Title);
                if (distance >= lowestDistance)
                {
                    continue;
                }

                lowestDistance = distance;
                bestMatch = track;
            }
            
            queue.Add(bestMatch);

            if (queue.Length > 1)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Added Track")
                    .WithThumbnailUrl($"https://img.youtube.com/vi/{bestMatch.Id}/0.jpg")
                    .AddField("Track", $"[{bestMatch.Title}]({bestMatch.Url})")
                    // .AddField("Estimated time until played", "n")
                    .AddField("Track length", bestMatch.Duration, true)
                    // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                    .AddField("Position in queue", queue.Length, true)
                    .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                    .Build();

                await ReplyAsync(embed: embed);
            }
        }

        if (player.PlayerState is PlayerState.None or PlayerState.Stopped)
        {
            await player.PlayAsync(queue.GetNext());
        }
    }

    public enum BassBoost
    {
        None,
        Low,
        Medium,
        High
    }

    [Command("bassboost")]
    public async Task BassBoostAsync(BassBoost boost)
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

        var gain = MapBoost();

        for (var i = 0; i < 3; ++i)
        {
           await player.EqualizerAsync(new EqualizerBand(i, gain));
        }

        double MapBoost()
        {
            return boost switch
            {
                BassBoost.None => 0.0,
                BassBoost.Low => 0.20,
                BassBoost.Medium => 0.30,
                BassBoost.High => 0.35,
                _ => throw new ArgumentOutOfRangeException(nameof(boost), boost, null)
            };
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
        
        await player.StopAsync();
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
        if (queue.Length == 0)
        {
            await ReplyAsync(embed: Embeds.Error("The queue is empty"));

            return;
        }
        
        var embed = new EmbedBuilder()
            .WithDescription(
                string.Join(
                    Environment.NewLine,
                    queue.Select(
                        (track, index) =>
                            $"{(track == queue.Current ? "🎶 " : string.Empty)}{index + 1} - `[{track.Duration}]` [{track.Title.AsBold()}]({track.Url})")))
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("loop current")]
    [Alias("loop", "repeat", "repeat current")]
    public async Task LoopCurrentSong()
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
        queue.IsCurrentTrackLooped = !queue.IsCurrentTrackLooped;

        await ReplyAsync(embed: Embeds.Success($"I will {(queue.IsCurrentTrackLooped ? "now" : "no longer")} repeat the current track"));
    }

    [Command("loop queue")]
    [Alias("repeat queue")]
    public async Task LoopQueueAsync()
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
        queue.IsLooped = !queue.IsLooped;

        await ReplyAsync(embed: Embeds.Success($"I will {(queue.IsLooped ? "now" : "no longer")} repeat the queue"));
    }

    [Command("seek")]
    [Alias("wind to")]
    public async Task SeekAsync(string timestamp)
    {
        var player = _lavaNode.GetPlayer(Context.Guild);
        if (!TimeSpan.TryParse(timestamp, out var position))
        {
            await ReplyAsync(embed: Embeds.Error("Invalid timestamp"));

            return;
        }
        
        await player.SeekAsync(position);
    }

    [Command("forward")]
    [Alias("f")]
    public async Task ForwardAsync(int seconds)
    {
        var player = _lavaNode.GetPlayer(Context.Guild);

        var forwardedTimestamp = player.Track.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);
    }
    
    [Command("back")]
    [Alias("b", "rewind")]
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