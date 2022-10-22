using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Player.Filters;
using Victoria.Responses.Search;
using YouTubeSearch;

namespace Anivia.CommandModules;

[Name("Playback")]
public sealed class PlaybackModule : ModuleBase
{
    private static readonly Regex LinkRegex = new(
        "(http[s]?:\\/\\/(www\\.)?|ftp:\\/\\/(www\\.)?|www\\.){1}([0-9A-Za-z-\\.@:%_\\+~#=]+)+((\\.[a-zA-Z]{2,3})+)(/(.)*)?(\\?(.)*)?");
    
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
        
        // if (_lavaNode.HasPlayer(Context.Guild)/* && _lavaNode.GetPlayer(Context.Guild).VoiceChannel != voiceState.VoiceChannel*/)
        // {
        //     await ReplyAsync(embed: Embeds.Error("Music is already playing"));
        //
        //     return;
        // }

        var player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, (ITextChannel)Context.Channel);

        SearchResponse searchResponse;
        if (Uri.IsWellFormedUriString(song, UriKind.Absolute))
        {
            searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, song);
        }
        else
        {
            var search = new VideoSearch();
            var matches = await search.GetVideos(song, 1);

            var url = matches.First().getUrl();
            searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, matches.First().getUrl());
        }

        if (searchResponse.Status == SearchStatus.LoadFailed)
        {
            var reason = !string.IsNullOrWhiteSpace(searchResponse.Exception.Message)
                ? searchResponse.Exception.Message
                : "unknown";
            
            await ReplyAsync(embed: Embeds.Error($"Error loading track: {reason}"));

            return;
        }

        if (searchResponse.Status == SearchStatus.NoMatches)
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
                .AddField("Playlist length", track.Duration, true)
                // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                .AddField("Number of tracks", queue.Length, true)
                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                .Build();

            await ReplyAsync(embed: embed);
        }
        else
        {
            var bestMatch = searchResponse.Tracks.First(); 
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

    [Command("clear")]
    public async Task ClearAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (!_lavaNode.HasPlayer(Context.Guild))
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        _lavaNode.TryGetPlayer(Context.Guild, out var player);
        player.GetQueue().Clear();
        await player.StopAsync();

        await ReplyAsync(embed: Embeds.Success("Cleared the queue"));
    }

    [Command("move")]
    [Alias("mv")]
    public async Task MoveAsync(int from, int to)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (!_lavaNode.HasPlayer(Context.Guild))
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        _lavaNode.TryGetPlayer(Context.Guild, out var player);
        var queue = player.GetQueue();
        if (from < 1 || from > queue.Length || to < 1 || to > queue.Length)
        {
            await ReplyAsync(embed: Embeds.Error("Invalid indices"));

            return;
        }
        
        queue.Move(from, to);

        await ReplyAsync(embed: Embeds.Success("Track moved"));
    }

    public enum BassBoost
    {
        Mute,
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

        var player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, (ITextChannel)Context.Channel);

        var gain = MapBoost();

        await player.EqualizerAsync(Enumerable.Range(0, 3).Select(x => new EqualizerBand(x, gain)).ToArray());

        double MapBoost()
        {
            return boost switch
            {
                BassBoost.Mute => -0.25,
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        _lavaNode.TryGetPlayer(Context.Guild, out var player);
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
        _lavaNode.TryGetPlayer(Context.Guild, out var player);

        var forwardedTimestamp = player.Track.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);
    }
    
    [Command("back")]
    [Alias("b", "rewind")]
    public async Task BackAsync(int seconds)
    {
        _lavaNode.TryGetPlayer(Context.Guild, out var player);

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